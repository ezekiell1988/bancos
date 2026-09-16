using System.Globalization;
using System.Net;
using System.Text;

namespace Bancos.Mcp.Features.Reports;

public static class ReportHtmlRenderer
{
    private static readonly CultureInfo Crc = CultureInfo.GetCultureInfo("es-CR");

    private const string Styles = "<style>body{font-family:'Segoe UI',Arial,sans-serif;margin:2rem;color:#1a1a1a}"
        + "table{border-collapse:collapse;width:100%;margin-bottom:1.5rem}"
        + "th,td{border:1px solid #ccc;padding:.4rem .6rem;text-align:right}"
        + "th:first-child,td:first-child{text-align:left}"
        + ".meta{color:#555}.warning{color:#a15c00;font-weight:600}</style>";

    public static string RenderIncomeStatement(IncomeStatementReport report, DateTimeOffset generatedAt)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Estado de resultados</title>");
        html.Append(Styles);
        html.Append("</head><body>");
        html.Append($"<h1>Estado de resultados — {Encode(report.PeriodLabel)}</h1>");
        html.Append($"<p class=\"meta\">Período: {report.PeriodStart:yyyy-MM-dd} a {report.PeriodEnd:yyyy-MM-dd} · Moneda: CRC · Generado: {generatedAt:yyyy-MM-dd HH:mm} CR</p>");

        if (report.PendingClassificationCount > 0)
            html.Append($"<p class=\"warning\">Advertencia: hay {report.PendingClassificationCount} movimiento(s) sin clasificar en este período; los totales podrían cambiar.</p>");

        html.Append("<h2>Ingresos</h2>");
        html.Append(RenderCategoryTable(report.IncomeLines, report.TotalIncome));
        html.Append("<h2>Gastos</h2>");
        html.Append(RenderCategoryTable(report.ExpenseLines, report.TotalExpense));
        html.Append($"<h2>Resultado neto: {FormatAmount(report.NetResult)} CRC</h2>");
        html.Append("</body></html>");
        return html.ToString();
    }

    public static string RenderBalanceSheet(BalanceSheetReport report, DateTimeOffset generatedAt)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Situación financiera</title>");
        html.Append(Styles);
        html.Append("</head><body>");
        html.Append($"<h1>Situación financiera — {Encode(report.PeriodLabel)}</h1>");
        html.Append($"<p class=\"meta\">Al: {report.AsOfDate:yyyy-MM-dd} · Moneda: CRC · Generado: {generatedAt:yyyy-MM-dd HH:mm} CR</p>");

        if (report.AccountsMissingClosingCount > 0)
            html.Append($"<p class=\"warning\">Advertencia: hay {report.AccountsMissingClosingCount} cuenta(s) con movimientos pero sin cierre calculado para este período; ejecute calculate_period_closings.</p>");

        html.Append("<h2>Activos</h2>");
        html.Append(RenderAccountTable(report.AssetLines, report.TotalAssets));
        html.Append("<h2>Pasivos</h2>");
        html.Append(RenderAccountTable(report.LiabilityLines, report.TotalLiabilities));
        html.Append($"<h2>Capital: {FormatAmount(report.Equity)} CRC</h2>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static string RenderCategoryTable(IReadOnlyList<CategoryAmount> lines, decimal total)
    {
        var html = new StringBuilder("<table><thead><tr><th>Categoría</th><th>Monto (CRC)</th></tr></thead><tbody>");
        foreach (var line in lines)
            html.Append($"<tr><td>{Encode(line.CategoryName)}</td><td>{FormatAmount(line.AmountCrc)}</td></tr>");
        html.Append($"</tbody><tfoot><tr><th>Total</th><th>{FormatAmount(total)}</th></tr></tfoot></table>");
        return html.ToString();
    }

    private static string RenderAccountTable(IReadOnlyList<BalanceSheetAccountAmount> lines, decimal total)
    {
        var html = new StringBuilder("<table><thead><tr><th>Cuenta</th><th>Monto (CRC)</th></tr></thead><tbody>");
        foreach (var line in lines)
            html.Append($"<tr><td>{Encode(line.BankName)} — {Encode(line.AccountCode)}</td><td>{FormatAmount(line.AmountCrc)}</td></tr>");
        html.Append($"</tbody><tfoot><tr><th>Total</th><th>{FormatAmount(total)}</th></tr></tfoot></table>");
        return html.ToString();
    }

    private static string FormatAmount(decimal amount) => amount.ToString("N2", Crc);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
