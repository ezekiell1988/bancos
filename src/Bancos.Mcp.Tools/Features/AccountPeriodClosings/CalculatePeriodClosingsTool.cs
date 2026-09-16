using System.ComponentModel;
using Bancos.Mcp.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.AccountPeriodClosings;

[McpServerToolType]
public static class CalculatePeriodClosingsTool
{
    [McpServerTool(
        Name = "calculate_period_closings",
        Title = "Calcular cierres de saldo por periodo",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Encola un job Hangfire que calcula y persiste el saldo acumulado por cuenta bancaria desde el periodo indicado hacia adelante. Retorna el ID del job encolado.")]
    public static async Task<CalculatePeriodClosingsResult> CalculateAsync(
        [Description("ID del periodo desde el cual calcular los cierres (inclusive).")] Guid periodId,
        McpCatalogDbContext db,
        IBackgroundJobClient jobClient,
        CancellationToken cancellationToken)
    {
        var period = await db.Periods.FirstOrDefaultAsync(candidate => candidate.Id == periodId, cancellationToken);
        if (period is null)
            throw new McpException($"Período {periodId} no encontrado.");

        var warnings = new List<string>();
        if (!await db.Transactions.AnyAsync(transaction => transaction.PeriodId == periodId, cancellationToken))
            warnings.Add("El período no tiene movimientos asignados; el job regenerará la asignación de períodos.");

        var jobId = jobClient.Enqueue<CalculateAccountPeriodClosingsJob>(
            job => job.ExecuteAsync(periodId, null!));

        return new CalculatePeriodClosingsResult("enqueued", periodId, period.Label, jobId, warnings);
    }
}

public sealed record CalculatePeriodClosingsResult(
    string Status,
    Guid PeriodId,
    string PeriodLabel,
    string JobId,
    IReadOnlyList<string> Warnings);
