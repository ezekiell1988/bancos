using System.Text.Json;
using Bancos.Mcp.Features.FileProcessing;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Hangfire;

namespace Bancos.Mcp.Features.Imports;

public sealed class RetryImportJobTool(ImportJobQueryService queryService, IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "retry_import_job",
        Title: "Reintentar un job de importación fallido",
        Description: "Reencola en Hangfire un job de importación que terminó en error, usando los mismos identificadores "
                   + "(ruta de archivo, plantilla y cuentas) que el job original; no reenvía bytes del archivo. "
                   + "Solo se permite cuando el job consultado está en estado 'error'.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                jobId = new
                {
                    type = "string",
                    description = "ID del job de Hangfire que falló."
                }
            },
            required = new[] { "jobId" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                originalJobId = new { type = "string" },
                newJobId = new { type = "string" },
                status = new { type = "string" }
            },
            required = new[] { "originalJobId", "newJobId", "status" },
            additionalProperties = false
        });

    public ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("jobId", out var jobIdEl) || jobIdEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(jobIdEl.GetString()))
            return ValueTask.FromResult(McpToolResult.Error("Se requiere 'jobId'."));

        var jobId = jobIdEl.GetString()!;
        var current = queryService.GetStatus(jobId);
        if (current.Status != "error")
            return ValueTask.FromResult(McpToolResult.Error(
                $"Solo se puede reintentar un job en estado 'error'. Estado actual de {jobId}: '{current.Status}'."));

        var retryArgs = queryService.GetRetryArgs(jobId);
        if (retryArgs is null)
            return ValueTask.FromResult(McpToolResult.Error(
                $"No fue posible recuperar los parámetros originales del job {jobId} (puede haber expirado en Hangfire)."));

        if (!File.Exists(retryArgs.FilePath))
            return ValueTask.FromResult(McpToolResult.Error(
                $"El archivo original ya no existe en el servidor: {Path.GetFileName(retryArgs.FilePath)}."));

        using var scope = scopeFactory.CreateScope();
        var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        // ExecuteAsync solo llama SaveChangesAsync al final, así que un intento fallido no deja escritura parcial: el reintento es seguro.
        var newJobId = jobClient.Enqueue<ImportFileJob>(job =>
            job.ExecuteAsync(retryArgs.FilePath, retryArgs.ParserKey, retryArgs.BankAccountId, retryArgs.UsdBankAccountId, null!));

        var result = new { originalJobId = jobId, newJobId, status = "en_cola" };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return ValueTask.FromResult(new McpToolResult([McpContent.FromText(json)], result));
    }
}
