using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Classification;

[McpServerToolType]
public static class ApplyClassificationsFromMarkdownTool
{
    [McpServerTool(Name = "apply_classifications_from_markdown", Title = "Aplicar clasificaciones desde Markdown",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lee un Markdown de revisión generado por export_unclassified_transactions_markdown, "
               + "deduce la categoría de la columna 'Nota' de cada fila usando reglas de palabras clave "
               + "y llama confirm_transaction_classification internamente. "
               + "Retorna cuántas se aplicaron, cuántas se omitieron (nota vacía o sin coincidencia) "
               + "y la lista de omitidas con su nota para que el llamador las resuelva manualmente.")]
    public static async Task<ApplyClassificationsFromMarkdownResult> ApplyClassificationsFromMarkdownAsync(
        ClassificationService service,
        IHostEnvironment environment,
        [Description("Ruta relativa dentro de docs del archivo Markdown a procesar.")] string relativePath,
        CancellationToken cancellationToken = default)
    {
        string filePath;
        try
        {
            filePath = UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath(environment.ContentRootPath, relativePath);
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message);
        }

        if (!File.Exists(filePath))
            throw new McpException($"Archivo no encontrado: {relativePath}");

        var rows = MarkdownClassificationParser.Parse(await File.ReadAllTextAsync(filePath, cancellationToken));

        var categories = await service.GetCategoriesAsync(cancellationToken);
        var categoryByCode = categories.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        int applied = 0, skipped = 0;
        var errors = new List<string>();
        var unresolved = new List<UnresolvedClassificationRow>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Note))
            {
                skipped++;
                continue;
            }

            if (!Guid.TryParse(row.TransactionId, out var transactionId))
            {
                errors.Add($"ID inválido '{row.TransactionId}'.");
                skipped++;
                continue;
            }

            var categoryCode = NoteToCategory.Resolve(row.Note);
            if (categoryCode is null || !categoryByCode.TryGetValue(categoryCode, out var category))
            {
                unresolved.Add(new UnresolvedClassificationRow(row.TransactionId, row.Note));
                skipped++;
                continue;
            }

            try
            {
                await service.ConfirmManualClassificationAsync(transactionId, category.Id, null, cancellationToken);
                applied++;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                errors.Add($"{row.TransactionId}: {ex.Message}");
                skipped++;
            }
        }

        return new ApplyClassificationsFromMarkdownResult(applied, skipped, unresolved, errors);
    }
}

public sealed record UnresolvedClassificationRow(string TransactionId, string Note);

public sealed record ApplyClassificationsFromMarkdownResult(
    int Applied,
    int Skipped,
    IReadOnlyList<UnresolvedClassificationRow> Unresolved,
    IReadOnlyList<string> Errors);
