using Bancos.Mcp.Features.Classification;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class UnclassifiedTransactionsMarkdownExporterTests
{
    [Fact]
    public void BuildMarkdown_creates_review_rows_without_internal_identifiers()
    {
        var transactions = new[]
        {
            new UnclassifiedTransactionSummary(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 1, 2),
                "COMPRA | PRUEBA",
                -1_250.5m,
                "CRC",
                "Pendiente")
        };

        var markdown = UnclassifiedTransactionsMarkdownExporter.BuildMarkdown(transactions);

        Assert.Contains("| M-001 | 2026-01-02 | COMPRA \\| PRUEBA | -1,250.50 | CRC |  |  |", markdown);
        Assert.DoesNotContain(transactions[0].TransactionId.ToString(), markdown);
        Assert.DoesNotContain(transactions[0].BankAccountId.ToString(), markdown);
    }

    [Theory]
    [InlineData("../fuera.md")]
    [InlineData("/tmp/fuera.md")]
    [InlineData("pendientes.txt")]
    public void ResolveOutputPath_rejects_invalid_paths(string relativePath)
    {
        Assert.Throws<ArgumentException>(() =>
            UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath("/repo/src/Bancos.Mcp", relativePath));
    }

    [Fact]
    public void ResolveOutputPath_keeps_file_inside_docs()
    {
        var path = UnclassifiedTransactionsMarkdownExporter.ResolveOutputPath(
            "/repo/src/Bancos.Mcp",
            "revisiones/pendientes.md");

        Assert.Equal("/repo/docs/revisiones/pendientes.md", path);
    }
}
