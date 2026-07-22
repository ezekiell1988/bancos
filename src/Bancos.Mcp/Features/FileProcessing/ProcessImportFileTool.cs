using System.Text.Json;
using Bancos.Mcp.Catalog;
using Bancos.Mcp.Features.TemplateDetection;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Hangfire;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed class ProcessImportFileTool(
    ImportTemplateDetectionService detectionService,
    IServiceScopeFactory scopeFactory,
    IBackgroundJobClient jobClient) : IMcpTool
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

        var jobs = new List<object>();
        foreach (var relativePath in relativePaths)
        {
            try
            {
                var templateId = await detectionService.DetectAsync(relativePath!, cancellationToken);
                var definition = ImportTemplateCatalog.Definitions.FirstOrDefault(d => d.Id == templateId)
                    ?? throw new InvalidOperationException("Plantilla no encontrada en catálogo.");

                var bankAccountId = await accountResolver.ResolveAsync(templateId, null, cancellationToken);
                var fullPath = detectionService.ResolveFullPath(relativePath!);

                var jobId = jobClient.Enqueue<ImportFileJob>(job =>
                    job.ExecuteAsync(fullPath, definition.ParserKey, bankAccountId, null!));

                jobs.Add(new { file = relativePath, jobId, template = definition.Code, status = "enqueued" });
            }
            catch (Exception ex)
            {
                jobs.Add(new { file = relativePath, jobId = (string?)null, template = (string?)null, status = "error", error = ex.Message });
            }
        }

        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)]);
    }
}
