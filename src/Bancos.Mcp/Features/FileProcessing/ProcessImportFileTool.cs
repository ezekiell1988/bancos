using System.Text;
using System.Text.Json;
using Bancos.Mcp.Catalog;
using Bancos.Mcp.Features.TemplateDetection;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Hangfire;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed class ProcessImportFileTool(
    ImportTemplateDetectionService detectionService,
    IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "process_import_file",
        Title: "Procesar archivos de importación bancaria",
        Description: "Recibe una lista de rutas relativas de archivos bancarios (CSV, XLS, PDF, HTML). "
                   + "Detecta automáticamente la plantilla, resuelve la cuenta bancaria y encola un job de Hangfire por archivo para parsear y persistir los datos. "
                   + "Devuelve en TOON el archivo y el jobId de cada job encolado; use get_import_job_status con ese jobId para saber si terminó o falló.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                files = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "Rutas relativas de archivos a procesar, ejemplo: [\"Coopealianza.pdf\", \"BAC_corte.csv\"]"
                }
            },
            required = new[] { "files" },
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
                            file = new { type = "string" },
                            jobId = new { type = new[] { "string", "null" } },
                            template = new { type = new[] { "string", "null" } },
                            status = new { type = "string" },
                            error = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "file", "jobId", "template", "status", "error" }
                    }
                }
            },
            required = new[] { "jobs" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("files", out var filesElement) || filesElement.ValueKind != JsonValueKind.Array)
            return McpToolResult.Error("Se requiere 'files' como array de rutas.");

        var relativePaths = filesElement.EnumerateArray()
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (relativePaths.Count == 0)
            return McpToolResult.Error("La lista de archivos está vacía.");

        using var scope = scopeFactory.CreateScope();
        var accountResolver = scope.ServiceProvider.GetRequiredService<AccountResolver>();
        var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var jobs = new List<ProcessedFileJob>();
        foreach (var relativePath in relativePaths)
        {
            try
            {
                var templateId = await detectionService.DetectAsync(relativePath!, cancellationToken);
                var definition = ImportTemplateCatalog.Definitions.FirstOrDefault(d => d.Id == templateId)
                    ?? throw new InvalidOperationException("Plantilla no encontrada en catálogo.");

                var fullPath = detectionService.ResolveFullPath(relativePath!);
                var fileContent = await File.ReadAllBytesAsync(fullPath, cancellationToken);

                Guid primaryAccountId;
                Guid? secondaryAccountId = null;
                if (definition.ParserKey == "bac-credit-financing-xls")
                {
                    var pair = await accountResolver.ResolveFinancingPairByPathAsync(relativePath!, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey == "bac-credit-online-pdf")
                {
                    primaryAccountId = await accountResolver.ResolveCrcByPathAsync(relativePath!, cancellationToken);
                }
                else if (definition.ParserKey == "bac-credit-csv")
                {
                    var pair = await accountResolver.ResolveFinancingPairByPathAsync(relativePath!, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey == "bn-card-statement-pdf")
                {
                    var pair = await accountResolver.ResolveBnCardStatementPairAsync(
                        templateId, fileContent, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey is "bcr-debit-csv" or "bn-debit-csv" or "bn-debit-csv-crc")
                {
                    var resolved = await accountResolver.TryResolveDebitCsvByIbanPathAsync(
                        relativePath!, cancellationToken);
                    if (resolved.HasValue)
                    {
                        primaryAccountId = resolved.Value.AccountId;
                        templateId = resolved.Value.TemplateId;
                        definition = ImportTemplateCatalog.Definitions.First(d => d.Id == templateId);
                    }
                    else
                    {
                        primaryAccountId = await accountResolver.ResolveAsync(
                            templateId, null, fileContent, cancellationToken);
                    }
                }
                else if (definition.ParserKey == "bank-account-movements-xls")
                {
                    primaryAccountId = await accountResolver.ResolveLinkedAccountByIbanPathAsync(
                        relativePath!, templateId, cancellationToken);
                }
                else
                {
                    primaryAccountId = await accountResolver.ResolveAsync(templateId, null, fileContent, cancellationToken);
                }

                var jobId = jobClient.Enqueue<ImportFileJob>(job =>
                    job.ExecuteAsync(fullPath, definition.ParserKey, primaryAccountId, secondaryAccountId, null!));

                jobs.Add(new ProcessedFileJob(relativePath!, jobId, definition.Code, "enqueued", null));
            }
            catch (Exception ex)
            {
                jobs.Add(new ProcessedFileJob(relativePath!, null, null, "error", ex.Message));
            }
        }

        var toon = FormatToon(jobs);
        var structured = new
        {
            jobs = jobs.Select(j => new { j.File, j.JobId, j.Template, j.Status, j.Error })
        };
        return new McpToolResult([McpContent.FromText(toon)], structured);
    }

    private sealed record ProcessedFileJob(string File, string? JobId, string? Template, string Status, string? Error);

    private static string FormatToon(IReadOnlyList<ProcessedFileJob> jobs)
    {
        var output = new StringBuilder()
            .AppendLine("format:toon")
            .AppendLine($"jobs[{jobs.Count}]{{file,jobId,template,status,error}}:");

        foreach (var job in jobs)
        {
            output.Append(Value(job.File)).Append(',')
                .Append(Value(job.JobId ?? "")).Append(',')
                .Append(Value(job.Template ?? "")).Append(',')
                .Append(Value(job.Status)).Append(',')
                .Append(Value(job.Error ?? "")).AppendLine();
        }

        return output.ToString();
    }

    private static string Value(string value) =>
        value.IndexOfAny([',', '"', '\\', '\n', '\r']) >= 0 || value.StartsWith(' ') || value.EndsWith(' ')
            ? $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")}\""
            : value;
}
