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
    string Explanation,
    string BankName,
    string AccountCode);

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

        TransactionClassification classification;
        if (match is not null)
        {
            if (string.IsNullOrWhiteSpace(transaction.Place) && !string.IsNullOrWhiteSpace(match.Place))
            {
                transaction.Place = match.Place;
                transaction.UpdatedAt = CostaRicaTime.Now;
            }

            classification = new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                CategoryId = match.CategoryId,
                ClassificationRuleId = match.Id,
                Source = ClassificationSource.Rule,
                Confidence = 1m,
                Explanation = $"Coincidencia determinista con la regla \"{match.DescriptionPattern}\" ({match.MatchType})."
            };
        }
        else
        {
            classification = await TryClassifyWithAiAsync(transaction, ct) ?? new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                Source = ClassificationSource.Unclassified,
                Explanation = aiOptions.Value.Enabled
                    ? "Ninguna regla determinista coincidió y la clasificación por IA no alcanzó el umbral de confianza o falló."
                    : "Ninguna regla determinista coincidió; clasificación por IA deshabilitada."
            };
        }

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
        Guid? bankAccountId, int page, int itemsPerPage, CancellationToken ct = default,
        string sortBy = "amount")
    {
        var query = db.Transactions.Where(t => !t.Classifications.Any(c => c.Source != ClassificationSource.Unclassified));
        if (bankAccountId is not null)
            query = query.Where(t => t.BankAccountId == bankAccountId);

        var totalItems = await query.CountAsync(ct);
        var withIncludes = query
            .Include(t => t.BankAccount)
                .ThenInclude(a => a!.Bank);
        var ordered = sortBy == "date"
            ? withIncludes.OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            : withIncludes.OrderBy(t => t.CurrencyCode)
                          .ThenByDescending(t => t.Amount < 0 ? -t.Amount : t.Amount)
                          .ThenBy(t => t.Id);
        var transactions = await ordered
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
                latestExplanationByTransaction.GetValueOrDefault(t.Id) ?? "Sin intento de clasificación registrado.",
                t.BankAccount?.Bank?.Name ?? "Desconocido",
                t.BankAccount?.Code ?? "Desconocido"))
            .ToList();

        return new UnclassifiedTransactionsPage(items, page, itemsPerPage, totalItems);
    }

    public async Task<TransactionClassification> ConfirmManualClassificationAsync(
        Guid transactionId, Guid categoryId, string? place = null, CancellationToken ct = default)
    {
        var transaction = await db.Transactions.FindAsync([transactionId], ct)
            ?? throw new InvalidOperationException($"Movimiento {transactionId} no encontrado.");

        _ = await db.Categories.FindAsync([categoryId], ct)
            ?? throw new InvalidOperationException($"Categoría {categoryId} no encontrada.");

        var normalizedPlace = string.IsNullOrWhiteSpace(place) ? null : place.Trim();
        if (normalizedPlace?.Length > 120)
            throw new ArgumentException("'place' no puede exceder 120 caracteres.");

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
                Place = normalizedPlace,
                Priority = 10
            };
            db.ClassificationRules.Add(rule);
        }
        else
        {
            rule.CategoryId = categoryId;
            rule.IsEnabled = true;
            rule.UpdatedAt = CostaRicaTime.Now;
            if (normalizedPlace is not null)
                rule.Place = normalizedPlace;
        }

        if (normalizedPlace is not null)
        {
            transaction.Place = normalizedPlace;
            transaction.UpdatedAt = CostaRicaTime.Now;
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

    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Categories
            .Where(c => c.IsEnabled && c.ParentId != null)
            .Select(c => new CategorySummary(c.Id, c.Code, c.Name))
            .ToListAsync(ct);
}

public sealed record CategorySummary(Guid Id, string Code, string Name);
