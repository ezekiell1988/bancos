using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reconciliation;

public sealed class ProposeReconciliationTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "propose_reconciliation",
        Title: "Proponer conciliación de pagos y transferencias",
        Description: "Calcula una propuesta N:N determinista a partir de partidas seleccionadas y explica montos, fechas y confianza.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                paymentTransactionIds = new { type = "array", items = new { type = "string", format = "uuid" }, minItems = 1 },
                transferTransactionIds = new { type = "array", items = new { type = "string", format = "uuid" }, minItems = 1 }
            },
            required = new[] { "paymentTransactionIds", "transferTransactionIds" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!ReconciliationToolSupport.TryReadGuidArray(arguments, "paymentTransactionIds", out var paymentIds, out var paymentError))
            return McpToolResult.Error(paymentError!);
        if (!ReconciliationToolSupport.TryReadGuidArray(arguments, "transferTransactionIds", out var transferIds, out var transferError))
            return McpToolResult.Error(transferError!);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        try
        {
            return ReconciliationToolSupport.JsonResult(await service.ProposeAsync(paymentIds, transferIds, cancellationToken));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}