using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Imports;

public sealed class GetImportJobStatusTool(ImportJobQueryService queryService) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_import_job_status",
        Title: "Estado de un job de importación",
        Description: "Consulta en Hangfire el estado de un job encolado por process_import_file usando su jobId: "
                   + "si sigue en cola/procesando, si terminó (con el resumen del resultado) o si falló (con el mensaje y detalle del error para revisarlo). "
                   + "Los jobs expiran de Hangfire tras su retención por defecto (1 día).",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                jobId = new
                {
                    type = "string",
                    description = "ID del job de Hangfire devuelto por process_import_file."
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
                jobId = new { type = "string" },
                status = new { type = "string" },
                fileName = new { type = new[] { "string", "null" } },
                parserKey = new { type = new[] { "string", "null" } },
                enqueuedAt = new { type = new[] { "string", "null" } },
                startedAt = new { type = new[] { "string", "null" } },
                finishedAt = new { type = new[] { "string", "null" } },
                resultSummary = new { type = new[] { "string", "null" } },
                errorMessage = new { type = new[] { "string", "null" } },
                errorDetails = new { type = new[] { "string", "null" } },
                canRetry = new { type = "boolean" },
                nextStep = new { type = "string" }
            },
            required = new[]
            {
                "jobId", "status", "fileName", "parserKey", "enqueuedAt", "startedAt", "finishedAt",
                "resultSummary", "errorMessage", "errorDetails", "canRetry", "nextStep"
            },
            additionalProperties = false
        });

    public ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("jobId", out var jobIdEl) || jobIdEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(jobIdEl.GetString()))
            return ValueTask.FromResult(McpToolResult.Error("Se requiere 'jobId'."));

        var jobId = jobIdEl.GetString()!;
        var status = queryService.GetStatus(jobId);

        var result = new
        {
            jobId = status.JobId,
            status = status.Status,
            fileName = status.FileName,
            parserKey = status.ParserKey,
            enqueuedAt = status.EnqueuedAt?.ToString("O", CultureInfo.InvariantCulture),
            startedAt = status.StartedAt?.ToString("O", CultureInfo.InvariantCulture),
            finishedAt = status.FinishedAt?.ToString("O", CultureInfo.InvariantCulture),
            resultSummary = status.ResultSummary,
            errorMessage = status.ErrorMessage,
            errorDetails = status.ErrorDetails,
            canRetry = status.CanRetry,
            nextStep = status.NextStep
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return ValueTask.FromResult(new McpToolResult([McpContent.FromText(json)], result));
    }
}
