using System.Text;
using Bancos.Mcp.Features.Parsing;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class AccountMovementSpreadsheetParserTests
{
    [Fact]
    public void Normalizes_negative_values_in_separate_debit_and_credit_columns()
    {
        var html = """
            <table>
              <tr><th>Fecha contable</th><th>Descripción</th><th>Débitos</th><th>Créditos</th></tr>
              <tr><td>01/06/2026</td><td>Compra de prueba</td><td>-1,234.56</td><td></td></tr>
              <tr><td>02/06/2026</td><td>Crédito de prueba</td><td></td><td>789.10</td></tr>
            </table>
            """;

        var movements = new AccountMovementSpreadsheetParser().Parse(Encoding.UTF8.GetBytes(html));

        Assert.Equal(2, movements.Count);
        Assert.Equal(1234.56m, movements[0].Debit);
        Assert.Equal(0m, movements[0].Credit);
        Assert.Equal(0m, movements[1].Debit);
        Assert.Equal(789.10m, movements[1].Credit);
    }

    [Fact]
    public void Preserves_sign_semantics_for_a_single_amount_column()
    {
        var html = """
            <table>
              <tr><th>Fecha</th><th>Descripción</th><th>Monto</th></tr>
              <tr><td>01/06/2026</td><td>Compra de prueba</td><td>-25.50</td></tr>
              <tr><td>02/06/2026</td><td>Crédito de prueba</td><td>10.25</td></tr>
            </table>
            """;

        var movements = new AccountMovementSpreadsheetParser().Parse(Encoding.UTF8.GetBytes(html));

        Assert.Equal(25.50m, movements[0].Debit);
        Assert.Equal(0m, movements[0].Credit);
        Assert.Equal(0m, movements[1].Debit);
        Assert.Equal(10.25m, movements[1].Credit);
    }
}
