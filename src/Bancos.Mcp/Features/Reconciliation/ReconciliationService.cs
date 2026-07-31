using System.Text.Json;
using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Reconciliation;

public sealed record UnreconciledTransaction(
    Guid TransactionId,
    DateOnly TransactionDate,
    string? ReferenceNumber,
    string Description,
    string BankCode,
    string AccountCode,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType);

public sealed record ReconciliationItemResult(
    Guid TransactionId,
    string Side,
    DateOnly TransactionDate,
    string Description,
    decimal AmountCrc);

public sealed record ReconciliationResult(
    Guid ReconciliationId,
    string Status,
    decimal Confidence,
    string Explanation,
    IReadOnlyList<ReconciliationItemResult> Items);

public sealed class ReconciliationService(McpCatalogDbContext db)
{
    public async Task<IReadOnlyList<UnreconciledTransaction>> ListUnreconciledAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int limit,
        CancellationToken ct = default)
    {
        var query = db.Transactions
            .AsNoTracking()
            .Where(transaction => !db.Set<ReconciliationItem>().Any(item =>
                item.TransactionId == transaction.Id &&
                item.Reconciliation!.Status == ReconciliationStatuses.Confirmed))
            .Include(transaction => transaction.BankAccount)
                .ThenInclude(account => account!.Bank)
            .AsQueryable();

        if (dateFrom is not null)
            query = query.Where(transaction => transaction.TransactionDate >= dateFrom.Value);
        if (dateTo is not null)
            query = query.Where(transaction => transaction.TransactionDate <= dateTo.Value);

        return await query
            .OrderBy(transaction => transaction.TransactionDate)
            .ThenBy(transaction => transaction.Id)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(transaction => new UnreconciledTransaction(
                transaction.Id,
                transaction.TransactionDate,
                transaction.ReferenceNumber,
                transaction.Description,
                transaction.BankAccount!.Code,
                transaction.BankAccount.Bank!.Code,
                transaction.CurrencyCode,
                transaction.Amount,
                transaction.AmountCrc,
                transaction.OperationType))
            .ToListAsync(ct);
    }

    public async Task<ReconciliationResult> ProposeAsync(
        IReadOnlyCollection<Guid> paymentTransactionIds,
        IReadOnlyCollection<Guid> transferTransactionIds,
        CancellationToken ct = default)
    {
        var paymentIds = NormalizeIds(paymentTransactionIds);
        var transferIds = NormalizeIds(transferTransactionIds);
        ValidateGroups(paymentIds, transferIds);

        var transactions = await LoadTransactionsAsync(paymentIds.Concat(transferIds), ct);
        EnsureAllTransactionsExist(transactions, paymentIds.Concat(transferIds));
        await EnsureNotConfirmedAsync(transactions.Select(transaction => transaction.Id), null, ct);

        var explanation = BuildExplanation(transactions, paymentIds, transferIds, out var confidence);
        var reconciliation = new Reconciliation
        {
            Id = Guid.NewGuid(),
            Confidence = confidence,
            Explanation = explanation
        };
        AddItems(reconciliation, transactions, paymentIds, ReconciliationSides.Payment);
        AddItems(reconciliation, transactions, transferIds, ReconciliationSides.Transfer);
        AddAudit(reconciliation, "proposed", "system", explanation, null);

        db.Add(reconciliation);
        await db.SaveChangesAsync(ct);
        return Map(reconciliation);
    }

    public async Task<ReconciliationResult> ConfirmAsync(
        Guid reconciliationId,
        string actor,
        string reason,
        CancellationToken ct = default)
    {
        ValidateAuditInput(actor, reason);
        var reconciliation = await LoadAsync(reconciliationId, ct)
            ?? throw new InvalidOperationException("La conciliación no existe.");
        if (reconciliation.Status == ReconciliationStatuses.Deleted)
            throw new InvalidOperationException("No se puede confirmar una conciliación eliminada.");

        await EnsureNotConfirmedAsync(reconciliation.Items.Select(item => item.TransactionId), reconciliationId, ct);
        reconciliation.Status = ReconciliationStatuses.Confirmed;
        reconciliation.ConfirmedAt = CostaRicaTime.Now;
        reconciliation.ConfirmedBy = actor.Trim();
        reconciliation.UpdatedAt = CostaRicaTime.Now;
        db.Add(AddAudit(reconciliation, "confirmed", actor, reason, Snapshot(reconciliation)));

        await db.SaveChangesAsync(ct);
        return Map(reconciliation);
    }

    public async Task<ReconciliationResult> CorrectAsync(
        Guid reconciliationId,
        IReadOnlyCollection<Guid> paymentTransactionIds,
        IReadOnlyCollection<Guid> transferTransactionIds,
        string actor,
        string reason,
        CancellationToken ct = default)
    {
        ValidateAuditInput(actor, reason);
        var paymentIds = NormalizeIds(paymentTransactionIds);
        var transferIds = NormalizeIds(transferTransactionIds);
        ValidateGroups(paymentIds, transferIds);

        var reconciliation = await LoadAsync(reconciliationId, ct)
            ?? throw new InvalidOperationException("La conciliación no existe.");
        if (reconciliation.Status == ReconciliationStatuses.Deleted)
            throw new InvalidOperationException("No se puede corregir una conciliación eliminada.");

        var transactions = await LoadTransactionsAsync(paymentIds.Concat(transferIds), ct);
        EnsureAllTransactionsExist(transactions, paymentIds.Concat(transferIds));
        await EnsureNotConfirmedAsync(transactions.Select(transaction => transaction.Id), reconciliationId, ct);

        var previousSnapshot = Snapshot(reconciliation);
        db.RemoveRange(reconciliation.Items);
        reconciliation.Items.Clear();
        var replacementItems = AddItems(reconciliation, transactions, paymentIds, ReconciliationSides.Payment);
        replacementItems.AddRange(AddItems(reconciliation, transactions, transferIds, ReconciliationSides.Transfer));
        db.AddRange(replacementItems);
        reconciliation.Confidence = CalculateConfidence(transactions, paymentIds, transferIds);
        reconciliation.Explanation = BuildExplanation(transactions, paymentIds, transferIds, out _);
        reconciliation.UpdatedAt = CostaRicaTime.Now;
        db.Add(AddAudit(reconciliation, "corrected", actor, reason, previousSnapshot));

        await db.SaveChangesAsync(ct);
        return Map(reconciliation);
    }

    public async Task<ReconciliationResult> DeleteAsync(
        Guid reconciliationId,
        string actor,
        string reason,
        CancellationToken ct = default)
    {
        ValidateAuditInput(actor, reason);
        var reconciliation = await LoadAsync(reconciliationId, ct)
            ?? throw new InvalidOperationException("La conciliación no existe.");
        if (reconciliation.Status == ReconciliationStatuses.Deleted)
            throw new InvalidOperationException("La conciliación ya está eliminada.");

        reconciliation.Status = ReconciliationStatuses.Deleted;
        reconciliation.UpdatedAt = CostaRicaTime.Now;
        db.Add(AddAudit(reconciliation, "deleted", actor, reason, Snapshot(reconciliation)));

        await db.SaveChangesAsync(ct);
        return Map(reconciliation);
    }

    private async Task<List<Transaction>> LoadTransactionsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        await db.Transactions
            .Where(transaction => ids.Contains(transaction.Id))
            .ToListAsync(ct);

    private async Task<Reconciliation?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Set<Reconciliation>()
            .Include(reconciliation => reconciliation.Items)
                .ThenInclude(item => item.Transaction)
            .SingleOrDefaultAsync(reconciliation => reconciliation.Id == id, ct);

    private async Task EnsureNotConfirmedAsync(IEnumerable<Guid> transactionIds, Guid? reconciliationId, CancellationToken ct)
    {
        var ids = transactionIds.Distinct().ToArray();
        var conflict = await db.Set<ReconciliationItem>().AnyAsync(item =>
            ids.Contains(item.TransactionId) &&
            item.Reconciliation!.Status == ReconciliationStatuses.Confirmed &&
            item.ReconciliationId != reconciliationId, ct);
        if (conflict)
            throw new InvalidOperationException("Una de las partidas ya pertenece a una conciliación confirmada.");
    }

    private static void EnsureAllTransactionsExist(IReadOnlyCollection<Transaction> transactions, IEnumerable<Guid> ids)
    {
        var missing = ids.Except(transactions.Select(transaction => transaction.Id)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"No se encontraron {missing.Length} partidas para conciliar.");
    }

    private static void ValidateGroups(IReadOnlyCollection<Guid> paymentIds, IReadOnlyCollection<Guid> transferIds)
    {
        if (paymentIds.Count == 0 || transferIds.Count == 0)
            throw new ArgumentException("Cada conciliación debe contener al menos un pago y una transferencia.");
        if (paymentIds.Intersect(transferIds).Any())
            throw new ArgumentException("Una partida no puede estar en ambos lados de la conciliación.");
    }

    private static void ValidateAuditInput(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Se requiere el actor de la operación.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Se requiere el motivo de la operación.");
    }

    private static Guid[] NormalizeIds(IEnumerable<Guid> ids) => ids.Distinct().ToArray();

    private static List<ReconciliationItem> AddItems(
        Reconciliation reconciliation,
        IEnumerable<Transaction> transactions,
        IEnumerable<Guid> ids,
        string side)
    {
        var byId = transactions.ToDictionary(transaction => transaction.Id);
        var items = new List<ReconciliationItem>();
        foreach (var id in ids)
        {
            var transaction = byId[id];
            var item = new ReconciliationItem
            {
                Id = Guid.NewGuid(),
                ReconciliationId = reconciliation.Id,
                TransactionId = id,
                Side = side,
                AmountCrc = Math.Abs(transaction.AmountCrc)
            };
            reconciliation.Items.Add(item);
            items.Add(item);
        }
        return items;
    }

    private static string BuildExplanation(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyCollection<Guid> paymentIds,
        IReadOnlyCollection<Guid> transferIds,
        out decimal confidence)
    {
        var paymentTotal = transactions.Where(transaction => paymentIds.Contains(transaction.Id)).Sum(transaction => Math.Abs(transaction.AmountCrc));
        var transferTotal = transactions.Where(transaction => transferIds.Contains(transaction.Id)).Sum(transaction => Math.Abs(transaction.AmountCrc));
        var allDates = transactions.Select(transaction => transaction.TransactionDate).ToArray();
        var dateFrom = allDates.Min();
        var dateTo = allDates.Max();
        confidence = CalculateConfidence(transactions, paymentIds, transferIds);
        var difference = Math.Abs(paymentTotal - transferTotal);
        return $"Pagos CRC {paymentTotal:N2}; transferencias CRC {transferTotal:N2}; diferencia CRC {difference:N2}; fechas {dateFrom:yyyy-MM-dd} a {dateTo:yyyy-MM-dd}; confianza {confidence:P0}.";
    }

    private static decimal CalculateConfidence(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyCollection<Guid> paymentIds,
        IReadOnlyCollection<Guid> transferIds)
    {
        var paymentTotal = transactions.Where(transaction => paymentIds.Contains(transaction.Id)).Sum(transaction => Math.Abs(transaction.AmountCrc));
        var transferTotal = transactions.Where(transaction => transferIds.Contains(transaction.Id)).Sum(transaction => Math.Abs(transaction.AmountCrc));
        var total = Math.Max(paymentTotal, transferTotal);
        var amountConfidence = total == 0 ? 0 : Math.Max(0m, 1m - Math.Abs(paymentTotal - transferTotal) / total);
        var dateSpan = transactions.Max(transaction => transaction.TransactionDate).DayNumber - transactions.Min(transaction => transaction.TransactionDate).DayNumber;
        var dateConfidence = Math.Max(0m, 1m - dateSpan / 30m);
        return decimal.Round(amountConfidence * 0.8m + dateConfidence * 0.2m, 4);
    }

    private static ReconciliationAudit AddAudit(Reconciliation reconciliation, string action, string actor, string? reason, string? snapshot)
    {
        var audit = new ReconciliationAudit
        {
            Id = Guid.NewGuid(),
            ReconciliationId = reconciliation.Id,
            Action = action,
            Actor = actor.Trim(),
            Reason = reason?.Trim(),
            SnapshotJson = snapshot
        };
        reconciliation.AuditEntries.Add(audit);
        return audit;
    }

    private static string Snapshot(Reconciliation reconciliation) => JsonSerializer.Serialize(new
    {
        reconciliation.Status,
        Items = reconciliation.Items.Select(item => new { item.TransactionId, item.Side, item.AmountCrc }).ToArray()
    });

    private static ReconciliationResult Map(Reconciliation reconciliation) => new(
        reconciliation.Id,
        reconciliation.Status,
        reconciliation.Confidence,
        reconciliation.Explanation,
        reconciliation.Items
            .OrderBy(item => item.Side)
            .ThenBy(item => item.TransactionId)
            .Select(item => new ReconciliationItemResult(
                item.TransactionId,
                item.Side,
                item.Transaction?.TransactionDate ?? default,
                item.Transaction?.Description ?? string.Empty,
                item.AmountCrc))
            .ToArray());
}