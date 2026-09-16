using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.TemplateDetection;

[McpServerToolType]
public static class DetectImportTemplateTool
{
    [McpServerTool(Name = "detect_import_template", Title = "Detectar plantilla de importación",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Identifica la plantilla de un archivo PDF, CSV, XLS o XLSX ubicado dentro del directorio de entrada local configurado. "
               + "Devuelve únicamente idImportTemplates y no persiste ni revela el contenido del archivo.")]
    public static async Task<DetectImportTemplateResult> DetectImportTemplateAsync(
        ImportTemplateDetectionService detectionService,
        [Description("Ruta relativa del archivo dentro del directorio de entrada configurado, por ejemplo: carpeta/archivo.csv. "
                   + "No se admiten rutas absolutas ni segmentos ..")] string relativePath,
        CancellationToken cancellationToken = default)
    {
        var templateId = await detectionService.DetectAsync(relativePath, cancellationToken);
        return new DetectImportTemplateResult(templateId);
    }
}

public sealed record DetectImportTemplateResult(Guid IdImportTemplates);
