using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Imports;

[McpServerToolType]
public static class GetImportJobStatusTool
{
    [McpServerTool(Name = "get_import_job_status", Title = "Estado de un job de importación",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Consulta en Hangfire el estado de un job encolado por process_import_file usando su jobId: "
               + "si sigue en cola/procesando, si terminó (con el resumen del resultado) o si falló (con el mensaje y detalle del error para revisarlo). "
               + "Los jobs expiran de Hangfire tras su retención por defecto (1 día).")]
    public static GetImportJobStatusResult GetImportJobStatus(
        ImportJobQueryService queryService,
        [Description("ID del job de Hangfire devuelto por process_import_file.")] string jobId)
    {
        var status = queryService.GetStatus(jobId);

        return new GetImportJobStatusResult(
            status.JobId,
            status.Status,
            status.FileName,
            status.ParserKey,
            status.EnqueuedAt?.ToString("O", CultureInfo.InvariantCulture),
            status.StartedAt?.ToString("O", CultureInfo.InvariantCulture),
            status.FinishedAt?.ToString("O", CultureInfo.InvariantCulture),
            status.ResultSummary,
            status.ErrorMessage,
            status.ErrorDetails,
            status.CanRetry,
            status.NextStep);
    }
}

public sealed record GetImportJobStatusResult(
    string JobId,
    string Status,
    string? FileName,
    string? ParserKey,
    string? EnqueuedAt,
    string? StartedAt,
    string? FinishedAt,
    string? ResultSummary,
    string? ErrorMessage,
    string? ErrorDetails,
    bool CanRetry,
    string NextStep);
