using System.Globalization;
using System.Text;

namespace Bancos.Mcp.Features.Reports;

internal static class ReportToonFormatter
{
    public static string FormatIncomeStatement(IncomeStatementReport report)
    {
        var previous = report.PreviousPeriod;
        var incomeLines = MergeCategoryLines(report.IncomeLines, previous?.IncomeLines);
        var expenseLines = MergeCategoryLines(report.ExpenseLines, previous?.ExpenseLines);
        var output = new StringBuilder()
            .AppendLine("format:toon")
            .AppendLine("report:income_statement")
            .AppendLine("currency:CRC")
            .AppendLine($"periodId:{report.PeriodId}")
            .AppendLine($"periodLabel:{Value(report.PeriodLabel)}")
            .AppendLine($"periodStart:{report.PeriodStart:yyyy-MM-dd}")
            .AppendLine($"periodEnd:{report.PeriodEnd:yyyy-MM-dd}")
            .AppendLine($"previousPeriodId:{(previous is null ? "none" : previous.PeriodId)}")
            .AppendLine($"previousPeriodLabel:{(previous is null ? "none" : Value(previous.PeriodLabel))}")
            .AppendLine($"incomeLines[{incomeLines.Count}]{{categoryCode,categoryName,amountCrc,previousAmountCrc,varianceCrc}}:");

        foreach (var line in incomeLines)
            AppendCategoryLine(output, line);

        output.AppendLine($"expenseLines[{expenseLines.Count}]{{categoryCode,categoryName,amountCrc,previousAmountCrc,varianceCrc}}:");
        foreach (var line in expenseLines)
            AppendCategoryLine(output, line);

        output.AppendLine($"totalIncome:{Amount(report.TotalIncome)}")
            .AppendLine($"previousTotalIncome:{Amount(previous?.TotalIncome ?? 0m)}")
            .AppendLine($"varianceTotalIncome:{Amount(report.TotalIncome - (previous?.TotalIncome ?? 0m))}")
            .AppendLine($"totalExpense:{Amount(report.TotalExpense)}")
            .AppendLine($"previousTotalExpense:{Amount(previous?.TotalExpense ?? 0m)}")
            .AppendLine($"varianceTotalExpense:{Amount(report.TotalExpense - (previous?.TotalExpense ?? 0m))}")
            .AppendLine($"netResult:{Amount(report.NetResult)}")
            .AppendLine($"previousNetResult:{Amount(previous?.NetResult ?? 0m)}")
            .AppendLine($"varianceNetResult:{Amount(report.NetResult - (previous?.NetResult ?? 0m))}")
            .AppendLine($"pendingClassificationCount:{report.PendingClassificationCount}")
            .AppendLine($"previousPendingClassificationCount:{previous?.PendingClassificationCount ?? 0}");

        return output.ToString();
    }

    public static string FormatBalanceSheet(BalanceSheetReport report)
    {
        var previous = report.PreviousPeriod;
        var assetLines = MergeAccountLines(report.AssetLines, previous?.AssetLines);
        var liabilityLines = MergeAccountLines(report.LiabilityLines, previous?.LiabilityLines);
        var output = new StringBuilder()
            .AppendLine("format:toon")
            .AppendLine("report:balance_sheet")
            .AppendLine("currency:CRC")
            .AppendLine($"periodId:{report.PeriodId}")
            .AppendLine($"periodLabel:{Value(report.PeriodLabel)}")
            .AppendLine($"asOfDate:{report.AsOfDate:yyyy-MM-dd}")
            .AppendLine($"previousPeriodId:{(previous is null ? "none" : previous.PeriodId)}")
            .AppendLine($"previousPeriodLabel:{(previous is null ? "none" : Value(previous.PeriodLabel))}")
            .AppendLine($"assetLines[{assetLines.Count}]{{bankName,accountCode,amountCrc,previousAmountCrc,varianceCrc}}:");

        foreach (var line in assetLines)
            AppendAccountLine(output, line);

        output.AppendLine($"liabilityLines[{liabilityLines.Count}]{{bankName,accountCode,amountCrc,previousAmountCrc,varianceCrc}}:");
        foreach (var line in liabilityLines)
            AppendAccountLine(output, line);

        output.AppendLine($"totalAssets:{Amount(report.TotalAssets)}")
            .AppendLine($"previousTotalAssets:{Amount(previous?.TotalAssets ?? 0m)}")
            .AppendLine($"varianceTotalAssets:{Amount(report.TotalAssets - (previous?.TotalAssets ?? 0m))}")
            .AppendLine($"totalLiabilities:{Amount(report.TotalLiabilities)}")
            .AppendLine($"previousTotalLiabilities:{Amount(previous?.TotalLiabilities ?? 0m)}")
            .AppendLine($"varianceTotalLiabilities:{Amount(report.TotalLiabilities - (previous?.TotalLiabilities ?? 0m))}")
            .AppendLine($"equity:{Amount(report.Equity)}")
            .AppendLine($"previousEquity:{Amount(previous?.Equity ?? 0m)}")
            .AppendLine($"varianceEquity:{Amount(report.Equity - (previous?.Equity ?? 0m))}")
            .AppendLine($"balanceDifference:{Amount(report.BalanceDifference)}")
            .AppendLine($"previousBalanceDifference:{Amount(previous?.BalanceDifference ?? 0m)}")
            .AppendLine($"accountsMissingClosingCount:{report.AccountsMissingClosingCount}")
            .AppendLine($"previousAccountsMissingClosingCount:{previous?.AccountsMissingClosingCount ?? 0}");

        return output.ToString();
    }

    private static IReadOnlyList<CategoryComparisonLine> MergeCategoryLines(
        IReadOnlyList<CategoryAmount> current,
        IReadOnlyList<CategoryAmount>? previous)
    {
        var lines = current
            .Select(line => new CategoryComparisonLine(line.CategoryCode, line.CategoryName, line.AmountCrc, 0m))
            .ToDictionary(line => line.CategoryCode, StringComparer.Ordinal);

        foreach (var line in previous ?? [])
        {
            if (lines.TryGetValue(line.CategoryCode, out var currentLine))
                lines[line.CategoryCode] = currentLine with { PreviousAmountCrc = line.AmountCrc };
            else
                lines[line.CategoryCode] = new CategoryComparisonLine(line.CategoryCode, line.CategoryName, 0m, line.AmountCrc);
        }

        return lines.Values
            .OrderByDescending(line => Math.Max(line.AmountCrc, line.PreviousAmountCrc))
            .ThenBy(line => line.CategoryCode, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<AccountComparisonLine> MergeAccountLines(
        IReadOnlyList<BalanceSheetAccountAmount> current,
        IReadOnlyList<BalanceSheetAccountAmount>? previous)
    {
        var lines = current
            .Select(line => new AccountComparisonLine(line.BankName, line.AccountCode, line.AmountCrc, 0m))
            .ToDictionary(line => (line.BankName, line.AccountCode));

        foreach (var line in previous ?? [])
        {
            var key = (line.BankName, line.AccountCode);
            if (lines.TryGetValue(key, out var currentLine))
                lines[key] = currentLine with { PreviousAmountCrc = line.AmountCrc };
            else
                lines[key] = new AccountComparisonLine(line.BankName, line.AccountCode, 0m, line.AmountCrc);
        }

        return lines.Values
            .OrderByDescending(line => Math.Max(line.AmountCrc, line.PreviousAmountCrc))
            .ThenBy(line => line.BankName, StringComparer.Ordinal)
            .ThenBy(line => line.AccountCode, StringComparer.Ordinal)
            .ToList();
    }

    private static void AppendCategoryLine(StringBuilder output, CategoryComparisonLine line) => output
        .Append(Value(line.CategoryCode)).Append(',')
        .Append(Value(line.CategoryName)).Append(',')
        .Append(Amount(line.AmountCrc)).Append(',')
        .Append(Amount(line.PreviousAmountCrc)).Append(',')
        .Append(Amount(line.AmountCrc - line.PreviousAmountCrc)).AppendLine();

    private static void AppendAccountLine(StringBuilder output, AccountComparisonLine line) => output
        .Append(Value(line.BankName)).Append(',')
        .Append(Value(line.AccountCode)).Append(',')
        .Append(Amount(line.AmountCrc)).Append(',')
        .Append(Amount(line.PreviousAmountCrc)).Append(',')
        .Append(Amount(line.AmountCrc - line.PreviousAmountCrc)).AppendLine();

    private static string Amount(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Value(string value) =>
        value.IndexOfAny([',', '"', '\\', '\n', '\r']) >= 0 || value.StartsWith(' ') || value.EndsWith(' ')
            ? $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")}\""
            : value;

    private sealed record CategoryComparisonLine(string CategoryCode, string CategoryName, decimal AmountCrc, decimal PreviousAmountCrc);

    private sealed record AccountComparisonLine(string BankName, string AccountCode, decimal AmountCrc, decimal PreviousAmountCrc);
}
