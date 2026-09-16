using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Classification;

[McpServerToolType]
public static class ExportUnclassifiedTransactionsMarkdownTool
{
    [McpServerTool(Name = "export_unclassified_transactions_markdown", Title = "Exportar movimientos No clasificados a Markdown",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Genera de forma determinista un Markdown de revisión con todos los movimientos No clasificados. "
               + "La ruta debe ser relativa al directorio docs del repositorio; no usa LLM.")]
    public static async Task<ExportUnclassifiedTransactionsMarkdownResult> ExportUnclassifiedTransactionsMarkdownAsync(
        ClassificationService service,
        IHostEnvironment environment,
        [Description("Ruta relativa dentro de docs, por ejemplo: revisiones/pendientes.md.")] string relativePath,
        [Description("Criterio de ordenamiento: 'amount' (moneda y luego importe absoluto desc, por defecto) o 'date' (fecha asc).")] string sortBy = "amount",
        CancellationToken cancellationToken = default)
    {
        string outputPath;
        try
        {
            outputPath = UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath(environment.ContentRootPath, relativePath);
        }
        catch (ArgumentException exception)
        {
            throw new McpException(exception.Message);
        }

        sortBy = sortBy == "date" ? "date" : "amount";

        const int itemsPerPage = 200;
        var page = 1;
        var transactions = new List<UnclassifiedTransactionSummary>();
        while (true)
        {
            var result = await service.ListUnclassifiedAsync(null, page, itemsPerPage, cancellationToken, sortBy);
            transactions.AddRange(result.Items);
            if (transactions.Count >= result.TotalItems)
                break;
            page++;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(
            outputPath,
            UnclassifiedTransactionsMarkdownExporter.BuildMarkdown(transactions),
            cancellationToken);

        return new ExportUnclassifiedTransactionsMarkdownResult(relativePath, transactions.Count);
    }
}

public sealed record ExportUnclassifiedTransactionsMarkdownResult(string RelativePath, int ExportedItems);
