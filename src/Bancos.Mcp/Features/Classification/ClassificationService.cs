using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.Classification;

public sealed record UnclassifiedTransactionSummary(
    Guid TransactionId,
    Guid BankAccountId,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string Explanation);

public sealed record ClassifyBatchSummary(int Processed, int Rule, int Ai, int Unclassified);

public sealed record UnclassifiedTransactionsPage(
    IReadOnlyList<UnclassifiedTransactionSummary> Items,
    int Page,
    int ItemsPerPage,
    int TotalItems);

public sealed class ClassificationService(
    McpCatalogDbContext db,
    AzureAiClassifier aiClassifier,
    IOptions<ClassificationAiOptions> aiOptions)
{
    public async Task<TransactionClassification> ClassifyAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await db.Transactions.FindAsync([transactionId], ct)
            ?? throw new InvalidOperationException($"Movimiento {transactionId} no encontrado.");

        var candidateRules = await db.ClassificationRules
            .Where(rule => rule.IsEnabled && (rule.BankAccountId == null || rule.BankAccountId == transaction.BankAccountId))
            .ToListAsync(ct);

        var match = ClassificationRuleMatcher.FindBestMatch(candidateRules, transaction.Description, transaction.OperationType);

        var classification = match is not null
            ? new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                CategoryId = match.CategoryId,
                ClassificationRuleId = match.Id,
                Source = ClassificationSource.Rule,
                Confidence = 1m,
                Explanation = $"Coincidencia determinista con la regla \"{match.DescriptionPattern}\" ({match.MatchType})."
            }
            : await TryClassifyWithAiAsync(transaction, ct) ?? new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                Source = ClassificationSource.Unclassified,
                Explanation = aiOptions.Value.Enabled
                    ? "Ninguna regla determinista coincidió y la clasificación por IA no alcanzó el umbral de confianza o falló."
                    : "Ninguna regla determinista coincidió; clasificación por IA deshabilitada."
            };

        db.TransactionClassifications.Add(classification);
        await db.SaveChangesAsync(ct);
        return classification;
    }

    public async Task<ClassifyBatchSummary> ClassifyPendingAsync(Guid? bankAccountId, int limit, CancellationToken ct = default)
    {
        var query = db.Transactions.Where(t => !t.Classifications.Any());
        if (bankAccountId is not null)
            query = query.Where(t => t.BankAccountId == bankAccountId);

        var pendingIds = await query
            .OrderBy(t => t.TransactionDate)
            .Take(limit)
            .Select(t => t.Id)
            .ToListAsync(ct);

        int rule = 0, ai = 0, unclassified = 0;
        foreach (var id in pendingIds)
        {
            var classification = await ClassifyAsync(id, ct);
            switch (classification.Source)
            {
                case ClassificationSource.Rule: rule++; break;
                case ClassificationSource.Ai: ai++; break;
                default: unclassified++; break;
            }
        }

        return new ClassifyBatchSummary(pendingIds.Count, rule, ai, unclassified);
    }

    public async Task<UnclassifiedTransactionsPage> ListUnclassifiedAsync(
        Guid? bankAccountId, int page, int itemsPerPage, CancellationToken ct = default)
    {
        var query = db.Transactions.Where(t => !t.Classifications.Any(c => c.Source != ClassificationSource.Unclassified));
        if (bankAccountId is not null)
            query = query.Where(t => t.BankAccountId == bankAccountId);

        var totalItems = await query.CountAsync(ct);
        var transactions = await query
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync(ct);
        var transactionIds = transactions.Select(t => t.Id).ToList();

        var latestExplanationByTransaction = (await db.TransactionClassifications
                .Where(c => transactionIds.Contains(c.TransactionId) && c.Source == ClassificationSource.Unclassified)
                .ToListAsync(ct))
            .GroupBy(c => c.TransactionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First().Explanation);

        var items = transactions
            .Select(t => new UnclassifiedTransactionSummary(
                t.Id,
                t.BankAccountId,
                t.TransactionDate,
                t.Description,
                t.Amount,
                t.CurrencyCode,
                latestExplanationByTransaction.GetValueOrDefault(t.Id) ?? "Sin intento de clasificación registrado."))
            .ToList();

        return new UnclassifiedTransactionsPage(items, page, itemsPerPage, totalItems);
    }

    public async Task<TransactionClassification> ConfirmManualClassificationAsync(
        Guid transactionId, Guid categoryId, CancellationToken ct = default)
    {
        var transaction = await db.Transactions.FindAsync([transactionId], ct)
            ?? throw new InvalidOperationException($"Movimiento {transactionId} no encontrado.");

        _ = await db.Categories.FindAsync([categoryId], ct)
            ?? throw new InvalidOperationException($"Categoría {categoryId} no encontrada.");

        var rule = await db.ClassificationRules.FirstOrDefaultAsync(r =>
            r.BankAccountId == transaction.BankAccountId &&
            r.DescriptionPattern == transaction.Description &&
            r.MatchType == "exact" &&
            r.OperationType == transaction.OperationType, ct);

        if (rule is null)
        {
            rule = new ClassificationRule
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                BankAccountId = transaction.BankAccountId,
                DescriptionPattern = transaction.Description,
                MatchType = "exact",
                OperationType = transaction.OperationType,
                Priority = 10
            };
            db.ClassificationRules.Add(rule);
        }
        else
        {
            rule.CategoryId = categoryId;
            rule.IsEnabled = true;
            rule.UpdatedAt = CostaRicaTime.Now;
        }

        var classification = new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            CategoryId = categoryId,
            ClassificationRuleId = rule.Id,
            Source = ClassificationSource.Manual,
            Confidence = 1m,
            Explanation = "Confirmado manualmente por el usuario; se creó o actualizó una regla determinista para futuras coincidencias."
        };
        db.TransactionClassifications.Add(classification);
        await db.SaveChangesAsync(ct);
        return classification;
    }

    private async Task<TransactionClassification?> TryClassifyWithAiAsync(Transaction transaction, CancellationToken ct)
    {
        if (!aiOptions.Value.Enabled)
            return null;

        var categories = await db.Categories
            .Where(c => c.IsEnabled)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(ct);

        var normalizedDescription = AiDescriptionSanitizer.Sanitize(TextNormalizer.Normalize(transaction.Description));
        var suggestion = await aiClassifier.ClassifyAsync(
            normalizedDescription,
            categories.Select(c => (c.Code, c.Name)).ToList(),
            ct);

        if (suggestion is null)
            return null;

        var category = categories.FirstOrDefault(c => c.Code == suggestion.CategoryCode);
        if (category is null || suggestion.Confidence < (decimal)aiOptions.Value.MinimumConfidence)
            return null;

        return new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            CategoryId = category.Id,
            Source = ClassificationSource.Ai,
            Confidence = suggestion.Confidence,
            Explanation = $"Clasificado por IA (confianza {suggestion.Confidence:P0}): {suggestion.Reasoning}"
        };
    }
}
