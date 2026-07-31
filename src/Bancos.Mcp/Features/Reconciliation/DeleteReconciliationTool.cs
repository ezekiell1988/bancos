using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reconciliation;

public sealed class DeleteReconciliationTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "delete_reconciliation",
        Title: "Eliminar conciliación",
        Description: "Marca una conciliación como eliminada, conserva sus movimientos y registra la operación en auditoría.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                reconciliationId = new { type = "string", format = "uuid" },
                actor = new { type = "string", minLength = 1, maxLength = 120 },
                reason = new { type = "string", minLength = 1, maxLength = 500 }
            },
            required = new[] { "reconciliationId", "actor", "reason" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!ReconciliationToolSupport.TryReadGuid(arguments, "reconciliationId", out var id, out var idError))
            return McpToolResult.Error(idError!);
        if (!ReconciliationToolSupport.TryReadRequiredString(arguments, "actor", out var actor, out var actorError))
            return McpToolResult.Error(actorError!);
        if (!ReconciliationToolSupport.TryReadRequiredString(arguments, "reason", out var reason, out var reasonError))
            return McpToolResult.Error(reasonError!);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        try
        {
            return ReconciliationToolSupport.JsonResult(await service.DeleteAsync(id, actor, reason, cancellationToken));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}