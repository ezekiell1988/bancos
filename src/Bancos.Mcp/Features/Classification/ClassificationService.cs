using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Classification;

public sealed class ClassificationService(McpCatalogDbContext db)
{
    public async Task<TransactionClassification> ClassifyAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await db.Transactions.FindAsync([transactionId], ct)
            ?? throw new InvalidOperationException($"Movimiento {transactionId} no encontrado.");

        var candidateRules = await db.ClassificationRules
            .Where(rule => rule.IsEnabled && (rule.BankAccountId == null || rule.BankAccountId == transaction.BankAccountId))
            .ToListAsync(ct);

        var match = ClassificationRuleMatcher.FindBestMatch(candidateRules, transaction.Description, transaction.OperationType);

        var classification = match is null
            ? new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                Source = "unclassified"
            }
            : new TransactionClassification
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                CategoryId = match.CategoryId,
                ClassificationRuleId = match.Id,
                Source = "rule",
                Confidence = 1m,
                Explanation = $"Coincidencia determinista con la regla \"{match.DescriptionPattern}\" ({match.MatchType})."
            };

        db.TransactionClassifications.Add(classification);
        await db.SaveChangesAsync(ct);
        return classification;
    }
}
