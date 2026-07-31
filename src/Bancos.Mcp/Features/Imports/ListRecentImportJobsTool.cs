using System.Globalization;
using System.Text;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Imports;

public sealed class ListRecentImportJobsTool(ImportJobQueryService queryService) : IMcpTool
{
    private static readonly HashSet<string> AllowedStatuses = ["en_cola", "procesando", "completado", "error"];

    public McpToolDefinition Definition { get; } = new(
        Name: "list_recent_import_jobs",
        Title: "Listar jobs de importación recientes",
        Description: "Devuelve en TOON los jobs de importación recientes conocidos por Hangfire (en cola, procesando, completados o con error), "
                   + "con su archivo, estado y resumen/error. Útil para revisar duplicados (resultado 'Duplicado detectado…') sin recorrer archivo por archivo. "
                   + "Solo incluye jobs que Hangfire aún conserva (retención por defecto: 1 día).",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                statusFilter = new
                {
                    type = new[] { "string", "null" },
                    @enum = new object?[] { "en_cola", "procesando", "completado", "error", null },
                    description = "Filtra por estado; omite para incluir todos."
                },
                itemsPerPage = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 200,
                    description = "Cantidad máxima de jobs a devolver (por defecto 50; máximo 200)."
                }
            },
            required = Array.Empty<string>(),
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                jobs = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            jobId = new { type = "string" },
                            fileName = new { type = new[] { "string", "null" } },
                            status = new { type = "string" },
                            at = new { type = new[] { "string", "null" } },
                            detail = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "jobId", "fileName", "status", "at", "detail" }
                    }
                }
            },
            required = new[] { "jobs" },
            additionalProperties = false
        });

    public ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string? statusFilter = null;
        if (arguments.TryGetProperty("statusFilter", out var statusEl) && statusEl.ValueKind == JsonValueKind.String)
        {
            statusFilter = statusEl.GetString();
            if (statusFilter is not null && !AllowedStatuses.Contains(statusFilter))
                return ValueTask.FromResult(McpToolResult.Error(
                    "statusFilter debe ser uno de: en_cola, procesando, completado, error."));
        }

        var itemsPerPage = 50;
        if (arguments.TryGetProperty("itemsPerPage", out var itemsPerPageEl) && itemsPerPageEl.ValueKind == JsonValueKind.Number)
            itemsPerPage = Math.Clamp(itemsPerPageEl.GetInt32(), 1, 200);

        var jobs = queryService.ListRecent(itemsPerPage, statusFilter);
        var toon = FormatToon(jobs);
        var structured = new
        {
            jobs = jobs.Select(j => new
            {
                jobId = j.JobId,
                fileName = j.FileName,
                status = j.Status,
                at = j.At?.ToString("O", CultureInfo.InvariantCulture),
                detail = j.Detail
            })
        };
        return ValueTask.FromResult(new McpToolResult([McpContent.FromText(toon)], structured));
    }

    private static string FormatToon(IReadOnlyList<RecentImportJob> jobs)
    {
        var output = new StringBuilder()
            .AppendLine("format:toon")
            .AppendLine($"jobs[{jobs.Count}]{{jobId,fileName,status,at,detail}}:");

        foreach (var job in jobs)
        {
            output.Append(Value(job.JobId)).Append(',')
                .Append(Value(job.FileName ?? "")).Append(',')
                .Append(Value(job.Status)).Append(',')
                .Append(Value(job.At?.ToString("O", CultureInfo.InvariantCulture) ?? "")).Append(',')
                .Append(Value(job.Detail ?? "")).AppendLine();
        }

        return output.ToString();
    }

    private static string Value(string value) =>
        value.IndexOfAny([',', '"', '\\', '\n', '\r']) >= 0 || value.StartsWith(' ') || value.EndsWith(' ')
            ? $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")}\""
            : value;
}
