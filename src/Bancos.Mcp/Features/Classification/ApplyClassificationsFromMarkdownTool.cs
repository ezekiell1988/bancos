using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ApplyClassificationsFromMarkdownTool(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "apply_classifications_from_markdown",
        Title: "Aplicar clasificaciones desde Markdown",
        Description: "Lee un Markdown de revisión generado por export_unclassified_transactions_markdown, "
                   + "deduce la categoría de la columna 'Nota' de cada fila usando reglas de palabras clave "
                   + "y llama confirm_transaction_classification internamente. "
                   + "Retorna cuántas se aplicaron, cuántas se omitieron (nota vacía o sin coincidencia) "
                   + "y la lista de omitidas con su nota para que el llamador las resuelva manualmente.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                relativePath = new
                {
                    type = "string",
                    description = "Ruta relativa dentro de docs del archivo Markdown a procesar."
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
                applied = new { type = "integer" },
                skipped = new { type = "integer" },
                unresolved = new { type = "array", items = new { type = "object" } },
                errors = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "applied", "skipped", "unresolved", "errors" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("relativePath", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return McpToolResult.Error("'relativePath' es requerido.");

        string filePath;
        try
        {
            filePath = UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath(
                environment.ContentRootPath, pathEl.GetString()!);
        }
        catch (ArgumentException ex)
        {
            return McpToolResult.Error(ex.Message);
        }

        if (!File.Exists(filePath))
            return McpToolResult.Error($"Archivo no encontrado: {pathEl.GetString()}");

        var rows = MarkdownClassificationParser.Parse(await File.ReadAllTextAsync(filePath, cancellationToken));

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();
        var categories = await service.GetCategoriesAsync(cancellationToken);
        var categoryByCode = categories.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        int applied = 0, skipped = 0;
        var errors = new List<string>();
        var unresolved = new List<object>();

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
                unresolved.Add(new { transactionId = row.TransactionId, note = row.Note });
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

        var result = new { applied, skipped, unresolved, errors };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
