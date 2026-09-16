using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Imports;

[McpServerToolType]
public static class ListRecentImportJobsTool
{
    private static readonly HashSet<string> AllowedStatuses = ["en_cola", "procesando", "completado", "error"];

    [McpServerTool(Name = "list_recent_import_jobs", Title = "Listar jobs de importación recientes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve los jobs de importación recientes conocidos por Hangfire (en cola, procesando, completados o con error), "
               + "con su archivo, estado y resumen/error. Útil para revisar duplicados (resultado 'Duplicado detectado…') sin recorrer archivo por archivo. "
               + "Solo incluye jobs que Hangfire aún conserva (retención por defecto: 1 día).")]
    public static ListRecentImportJobsResult ListRecentImportJobs(
        ImportJobQueryService queryService,
        [Description("Filtra por estado (en_cola, procesando, completado, error); omite para incluir todos.")] string? statusFilter = null,
        [Description("Cantidad máxima de jobs a devolver (por defecto 50; máximo 200).")] int itemsPerPage = 50)
    {
        if (statusFilter is not null && !AllowedStatuses.Contains(statusFilter))
            throw new McpException("statusFilter debe ser uno de: en_cola, procesando, completado, error.");

        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var jobs = queryService.ListRecent(itemsPerPage, statusFilter);
        var items = jobs.Select(j => new RecentImportJobItem(
            j.JobId,
            j.FileName,
            j.Status,
            j.At?.ToString("O", CultureInfo.InvariantCulture),
            j.Detail)).ToList();

        return new ListRecentImportJobsResult(items);
    }
}

public sealed record RecentImportJobItem(string JobId, string? FileName, string Status, string? At, string? Detail);

public sealed record ListRecentImportJobsResult(IReadOnlyList<RecentImportJobItem> Jobs);
