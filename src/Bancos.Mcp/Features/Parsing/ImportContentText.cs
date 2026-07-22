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
        return string.Join('\n', document.GetPages().Select(page => page.Text));
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
