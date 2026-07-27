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
                   + "Retorna el ID de cada job encolado. No requiere ningún parámetro adicional.",
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
                            status = new { type = "string" }
                        },
                        required = new[] { "file", "jobId", "template", "status" }
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

        var jobs = new List<object>();
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
                else if (definition.ParserKey == "bcr-debit-csv")
                {
                    var alt = await accountResolver.TryResolveAlternativeByIbanPathAsync(relativePath!, templateId, cancellationToken);
                    if (alt.HasValue)
                    {
                        templateId = alt.Value.TemplateId;
                        definition = ImportTemplateCatalog.Definitions.First(d => d.Id == templateId);
                        primaryAccountId = alt.Value.AccountId;
                    }
                    else
                    {
                        primaryAccountId = await accountResolver.ResolveAsync(templateId, null, fileContent, cancellationToken);
                    }
                }
                else
                {
                    primaryAccountId = await accountResolver.ResolveAsync(templateId, null, fileContent, cancellationToken);
                }

                var jobId = jobClient.Enqueue<ImportFileJob>(job =>
                    job.ExecuteAsync(fullPath, definition.ParserKey, primaryAccountId, secondaryAccountId, null!));

                jobs.Add(new { file = relativePath, jobId, template = definition.Code, status = "enqueued" });
            }
            catch (Exception ex)
            {
                jobs.Add(new { file = relativePath, jobId = (string?)null, template = (string?)null, status = "error", error = ex.Message });
            }
        }

        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], new { jobs });
    }
}
