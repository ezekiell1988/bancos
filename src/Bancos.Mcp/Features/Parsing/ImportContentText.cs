using System.Text;
using ExcelDataReader;
using UglyToad.PdfPig;

namespace Bancos.Mcp.Features.Parsing;

internal static class ImportContentText
{
    public static (string Kind, string Text) Extract(ReadOnlyMemory<byte> content)
    {
        var bytes = content.ToArray();
        if (bytes.AsSpan().StartsWith("%PDF-"u8)) return ("pdf", ExtractPdf(bytes));
        if (bytes.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 })) return ("xls", ExtractXls(bytes));
        var text = Encoding.UTF8.GetString(bytes);
        if (text.Contains("<html", StringComparison.OrdinalIgnoreCase) || text.Contains("<table", StringComparison.OrdinalIgnoreCase)) return ("html", text);
        return ("csv", text);
    }

    private static string ExtractPdf(byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);
        var pages = document.GetPages().Select(ReconstructPageText);
        return string.Join('\n', pages);
    }

    private static string ReconstructPageText(UglyToad.PdfPig.Content.Page page)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0) return page.Text;

        // Group words into lines by Y-coordinate proximity (within 3 points = same row)
        var rows = new List<List<UglyToad.PdfPig.Content.Word>>();
        foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left))
        {
            var placed = false;
            foreach (var row in rows)
            {
                var rowY = row[0].BoundingBox.Bottom;
                if (Math.Abs(word.BoundingBox.Bottom - rowY) <= 3)
                {
                    row.Add(word);
                    placed = true;
                    break;
                }
            }
            if (!placed) rows.Add([word]);
        }

        return string.Join('\n', rows.Select(row =>
            string.Join(' ', row.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))));
    }

    private static string ExtractXls(byte[] bytes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var values = new List<string>();
        do { while (reader.Read()) for (var column = 0; column < reader.FieldCount; column++) if (reader.GetValue(column) is { } value) values.Add(value.ToString()!); } while (reader.NextResult());
        return string.Join('\n', values);
    }
}
