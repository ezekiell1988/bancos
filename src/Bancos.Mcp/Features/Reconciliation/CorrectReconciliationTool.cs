using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reconciliation;

public sealed class CorrectReconciliationTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "correct_reconciliation",
        Title: "Corregir conciliación",
        Description: "Reemplaza las partidas asociadas a una conciliación, conserva los movimientos originales y registra la corrección.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                reconciliationId = new { type = "string", format = "uuid" },
                paymentTransactionIds = new { type = "array", items = new { type = "string", format = "uuid" }, minItems = 1 },
                transferTransactionIds = new { type = "array", items = new { type = "string", format = "uuid" }, minItems = 1 },
                actor = new { type = "string", minLength = 1, maxLength = 120 },
                reason = new { type = "string", minLength = 1, maxLength = 500 }
            },
            required = new[] { "reconciliationId", "paymentTransactionIds", "transferTransactionIds", "actor", "reason" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!ReconciliationToolSupport.TryReadGuid(arguments, "reconciliationId", out var id, out var idError))
            return McpToolResult.Error(idError!);
        if (!ReconciliationToolSupport.TryReadGuidArray(arguments, "paymentTransactionIds", out var paymentIds, out var paymentError))
            return McpToolResult.Error(paymentError!);
        if (!ReconciliationToolSupport.TryReadGuidArray(arguments, "transferTransactionIds", out var transferIds, out var transferError))
            return McpToolResult.Error(transferError!);
        if (!ReconciliationToolSupport.TryReadRequiredString(arguments, "actor", out var actor, out var actorError))
            return McpToolResult.Error(actorError!);
        if (!ReconciliationToolSupport.TryReadRequiredString(arguments, "reason", out var reason, out var reasonError))
            return McpToolResult.Error(reasonError!);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        try
        {
            return ReconciliationToolSupport.JsonResult(await service.CorrectAsync(id, paymentIds, transferIds, actor, reason, cancellationToken));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}