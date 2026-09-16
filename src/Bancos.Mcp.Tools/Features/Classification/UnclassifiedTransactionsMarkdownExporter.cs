using System.Globalization;
using System.Text;

namespace Bancos.Mcp.Features.Classification;

public static class UnclassifiedTransactionsMarkdownExporter
{
    public static string BuildMarkdown(IReadOnlyList<UnclassifiedTransactionSummary> transactions)
    {
        var markdown = new StringBuilder()
            .AppendLine("# Movimientos pendientes de clasificación")
            .AppendLine()
            .AppendLine($"Total: **{transactions.Count}** movimientos.")
            .AppendLine()
            .AppendLine("| ID | Fecha | Banco | Cuenta | Descripción | Importe | Moneda | Nota |")
            .AppendLine("|---|---|---|---|---|---:|---|---|");

        foreach (var transaction in transactions)
        {
            markdown.Append("| ")
                .Append(transaction.TransactionId.ToString())
                .Append(" | ")
                .Append(transaction.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(EscapeCell(transaction.BankName))
                .Append(" | ")
                .Append(EscapeCell(transaction.AccountCode))
                .Append(" | ")
                .Append(EscapeCell(transaction.Description))
                .Append(" | ")
                .Append(transaction.Amount.ToString("N2", CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(EscapeCell(transaction.CurrencyCode))
                .AppendLine(" |  |");
        }

        markdown.AppendLine()
            .AppendLine("## Cómo completar")
            .AppendLine()
            .AppendLine("- Escribe una nota breve en la columna **Nota** para los movimientos que reconoces.")
            .AppendLine("- Claude leerá las notas, deducirá la categoría y aplicará las clasificaciones en BD.")
            .AppendLine("- No es necesario anotar todos: los que quedan vacíos permanecen pendientes para la siguiente ronda.");

        return markdown.ToString();
    }

    public static string ResolveOutputPath(string contentRootPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("relativePath es requerido.");
        if (Path.IsPathRooted(relativePath) || ContainsTraversal(relativePath))
            throw new ArgumentException("La ruta debe ser relativa y permanecer dentro de docs.");
        if (!string.Equals(Path.GetExtension(relativePath), ".md", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("La exportación debe usar la extensión .md.");

        var outputRoot = Path.GetFullPath("../../docs", contentRootPath);
        var outputPath = Path.GetFullPath(relativePath, outputRoot);
        if (!outputPath.StartsWith(outputRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("La ruta debe permanecer dentro de docs.");

        return outputPath;
    }

    private static bool ContainsTraversal(string path) => path
        .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
        .Any(segment => segment == "..");

    private static string EscapeCell(string value) => value
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}
