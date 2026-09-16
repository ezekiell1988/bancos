using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Transactions;

[McpServerToolType]
public static class GetTransactionDetailTool
{
    [McpServerTool(Name = "get_transaction_detail", Title = "Detalle de movimiento",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve el detalle de un movimiento con su historial completo de clasificación (regla, IA, manual o sin clasificar) "
               + "para trazabilidad. No incluye IBAN, número de tarjeta ni credenciales.")]
    public static async Task<TransactionDetailResult> GetTransactionDetailAsync(
        TransactionsQueryService service,
        [Description("ID del movimiento a consultar.")] Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var detail = await service.GetDetailAsync(transactionId, cancellationToken)
            ?? throw new McpException($"Movimiento {transactionId} no encontrado.");

        var classifications = detail.Classifications.Select(c => new TransactionClassificationItem(
            c.Id,
            c.Source,
            c.CategoryCode,
            c.CategoryName,
            c.Confidence,
            c.Explanation,
            c.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            c.RuleDescriptionPattern)).ToList();

        return new TransactionDetailResult(
            detail.TransactionId,
            detail.BankAccountId,
            detail.BankName,
            detail.AccountCode,
            detail.PeriodId,
            detail.PeriodLabel,
            detail.ReferenceNumber,
            detail.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            detail.PaymentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            detail.Description,
            detail.Place,
            detail.CurrencyCode,
            detail.Amount,
            detail.AmountCrc,
            detail.ExchangeRate,
            detail.OperationType,
            classifications);
    }
}

public sealed record TransactionClassificationItem(
    Guid Id,
    string Source,
    string? CategoryCode,
    string? CategoryName,
    decimal? Confidence,
    string? Explanation,
    string CreatedAt,
    string? RuleDescriptionPattern);

public sealed record TransactionDetailResult(
    Guid TransactionId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    Guid? PeriodId,
    string? PeriodLabel,
    string? ReferenceNumber,
    string TransactionDate,
    string? PaymentDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    decimal? ExchangeRate,
    string OperationType,
    IReadOnlyList<TransactionClassificationItem> Classifications);
