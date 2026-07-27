using System.Globalization;
using System.Text;

namespace Bancos.Mcp.Features.Classification;

internal static class ToonFormatter
{
    public static string Format(UnclassifiedTransactionsPage page)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(page.TotalItems / (double)page.ItemsPerPage));
        var output = new StringBuilder()
            .AppendLine("format:toon")
            .AppendLine($"page:{page.Page}")
            .AppendLine($"itemsPerPage:{page.ItemsPerPage}")
            .AppendLine($"totalItems:{page.TotalItems}")
            .AppendLine($"totalPages:{totalPages}")
            .AppendLine($"transactions[{page.Items.Count}]{{transactionId,bankAccountId,transactionDate,description,amount,currencyCode,explanation}}:");

        foreach (var item in page.Items)
        {
            output.Append(item.TransactionId).Append(',')
                .Append(item.BankAccountId).Append(',')
                .Append(item.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                .Append(Value(item.Description)).Append(',')
                .Append(item.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Value(item.CurrencyCode)).Append(',')
                .Append(Value(item.Explanation)).AppendLine();
        }

        return output.ToString();
    }

    private static string Value(string value) =>
        value.IndexOfAny([',', '"', '\\', '\n', '\r']) >= 0 || value.StartsWith(' ') || value.EndsWith(' ')
            ? $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")}\""
            : value;
}
