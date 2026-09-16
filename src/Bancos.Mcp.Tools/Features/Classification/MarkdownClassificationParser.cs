namespace Bancos.Mcp.Features.Classification;

public sealed record MarkdownClassificationRow(
    string Ref,
    string TransactionId,
    string CategoryName,
    string Note);

public static class MarkdownClassificationParser
{
    // Expected columns: | ID | Fecha | Banco | Cuenta | Descripción | Importe | Moneda | Nota |
    //                      0    1       2       3        4              5         6        7
    private const int ColId = 0;
    private const int ColNote = 7;
    private const int MinCells = 10; // 8 cols + 2 empty boundary cells

    public static IReadOnlyList<MarkdownClassificationRow> Parse(string markdown)
    {
        var rows = new List<MarkdownClassificationRow>();
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('|') || trimmed.StartsWith("|---") || trimmed.StartsWith("| ID"))
                continue;

            var cells = trimmed.Split('|', StringSplitOptions.None);
            if (cells.Length < MinCells)
                continue;

            var id = cells[ColId + 1].Trim();
            var note = cells[ColNote + 1].Trim();

            if (!Guid.TryParse(id, out _))
                continue;

            rows.Add(new MarkdownClassificationRow(id, id, string.Empty, note));
        }
        return rows;
    }
}
