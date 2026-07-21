using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.TemplateDetection;

public sealed class DetectImportTemplateTool(ImportTemplateDetectionService detectionService) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "detect_import_template",
        Title: "Detectar plantilla de importación",
        Description: "Identifica la plantilla de un archivo PDF, CSV, XLS o XLSX ubicado dentro del directorio de entrada local configurado. Devuelve únicamente idImportTemplates y no persiste ni revela el contenido del archivo.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                relativePath = new
                {
                    type = "string",
                    description = "Ruta relativa del archivo dentro del directorio de entrada configurado, por ejemplo: carpeta/archivo.csv. No se admiten rutas absolutas ni segmentos .."
                }
            },
            required = new[] { "relativePath" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("relativePath", out var pathElement) || pathElement.ValueKind != JsonValueKind.String)
            throw new ArgumentException("relativePath es requerido.");

        var templateId = await detectionService.DetectAsync(pathElement.GetString()!, cancellationToken);
        return new McpToolResult([McpContent.FromText($"idImportTemplates: {templateId}")]);
    }
}
