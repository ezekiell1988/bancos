using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Ledger;

[McpServerToolType]
public static class GetLedgerPeriodTool
{
    [McpServerTool(
        Name = "get_ledger_period",
        Title = "Consultar libro mayor por período",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description(
        "Devuelve los comprobantes y líneas trazables de los movimientos registrados en un período. " +
        "Cada movimiento importado se representa como un comprobante de una línea porque el modelo actual conserva el auxiliar bancario, no asientos de doble partida.")]
    public static async Task<GetLedgerPeriodResult> GetLedgerPeriodAsync(
        [Description("ID del período que se desea consultar.")] Guid periodId,
        LedgerQueryService service,
        CancellationToken cancellationToken)
    {
        var ledger = await service.GetPeriodAsync(periodId, cancellationToken);
        if (ledger is null)
            throw new McpException($"Período {periodId} no encontrado.");

        return new GetLedgerPeriodResult(
            ledger.Warnings.Count == 0 ? "completed" : "completed_with_warnings",
            ledger.PeriodId,
            ledger.PeriodLabel,
            ledger.PeriodStart,
            ledger.PeriodEnd,
            ledger.Vouchers,
            ledger.Warnings);
    }
}

public sealed record GetLedgerPeriodResult(
    string Status,
    Guid PeriodId,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<LedgerVoucher> Vouchers,
    IReadOnlyList<string> Warnings);
