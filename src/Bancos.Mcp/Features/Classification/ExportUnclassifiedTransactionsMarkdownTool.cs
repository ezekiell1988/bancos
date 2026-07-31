using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ExportUnclassifiedTransactionsMarkdownTool(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "export_unclassified_transactions_markdown",
        Title: "Exportar movimientos No clasificados a Markdown",
        Description: "Genera de forma determinista un Markdown de revisión con todos los movimientos No clasificados. "
                   + "La ruta debe ser relativa al directorio docs del repositorio; no usa LLM.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                relativePath = new
                {
                    type = "string",
                    description = "Ruta relativa dentro de docs, por ejemplo: revisiones/pendientes.md."
                },
                sortBy = new
                {
                    type = "string",
                    @enum = new[] { "amount", "date" },
                    description = "Criterio de ordenamiento: 'amount' (moneda y luego importe absoluto desc, por defecto) o 'date' (fecha asc)."
                }
            },
            required = new[] { "relativePath" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                relativePath = new { type = "string" },
                exportedItems = new { type = "integer" }
            },
            required = new[] { "relativePath", "exportedItems" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("relativePath", out var pathElement) || pathElement.ValueKind != JsonValueKind.String)
            return McpToolResult.Error("'relativePath' es requerido.");

        string outputPath;
        try
        {
            outputPath = UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath(
                environment.ContentRootPath,
                pathElement.GetString()!);
        }
        catch (ArgumentException exception)
        {
            return McpToolResult.Error(exception.Message);
        }

        var sortBy = "amount";
        if (arguments.TryGetProperty("sortBy", out var sortEl) && sortEl.ValueKind == JsonValueKind.String)
            sortBy = sortEl.GetString() == "date" ? "date" : "amount";

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();
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

        var relativePath = pathElement.GetString()!;
        var response = new { relativePath, exportedItems = transactions.Count };
        return new McpToolResult(
            [McpContent.FromText($"Markdown generado: {relativePath}. Movimientos exportados: {transactions.Count}.")],
            response);
    }
}
