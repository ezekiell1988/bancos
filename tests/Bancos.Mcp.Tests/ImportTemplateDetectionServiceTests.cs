using System.IO.Compression;
using Bancos.Mcp.Catalog;
using Bancos.Mcp.Features.TemplateDetection;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ImportTemplateDetectionServiceTests : IDisposable
{
    private readonly string inputDirectory = Path.Combine(Path.GetTempPath(), $"bancos-mcp-detection-{Guid.NewGuid():N}");

    public ImportTemplateDetectionServiceTests() => Directory.CreateDirectory(inputDirectory);

    [Fact]
    public async Task Detects_a_csv_template_and_returns_its_catalog_id()
    {
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "statement.csv"), "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion");

        var id = await CreateService().DetectAsync("statement.csv", CancellationToken.None);

        Assert.Equal(Guid.Parse("10000000-0000-0000-0000-000000000001"), id);
    }

    [Fact]
    public async Task Detects_an_xlsx_template_and_returns_its_catalog_id()
    {
        CreateXlsx(Path.Combine(inputDirectory, "statement.xlsx"));

        var id = await CreateService().DetectAsync("statement.xlsx", CancellationToken.None);

        Assert.Equal(ImportTemplateCatalog.Definitions.Single(definition => definition.Code == "bank-account-movements-xls-v1").Id, id);
    }

    [Fact]
    public async Task Rejects_an_xlsx_that_exceeds_the_extraction_cell_limit()
    {
        CreateXlsx(Path.Combine(inputDirectory, "large.xlsx"));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            CreateService(maxSpreadsheetCells: 3).DetectAsync("large.xlsx", CancellationToken.None));

        Assert.Equal("El archivo supera los límites de extracción permitidos.", exception.Message);
    }

    [Theory]
    [InlineData("../statement.csv")]
    [InlineData("/tmp/statement.csv")]
    public async Task Rejects_paths_outside_the_configured_directory(string path)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => CreateService().DetectAsync(path, CancellationToken.None));

        Assert.DoesNotContain(path, exception.Message);
    }

    [Fact]
    public async Task Rejects_an_unsupported_file_extension()
    {
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "statement.txt"), "content");

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().DetectAsync("statement.txt", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_files_that_exceed_the_configured_size_limit()
    {
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "large.csv"), new string('a', 11));

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(maxFileSizeBytes: 10).DetectAsync("large.csv", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_a_symbolic_link_that_leaves_the_configured_directory()
    {
        var externalFile = Path.Combine(Path.GetTempPath(), $"bancos-mcp-external-{Guid.NewGuid():N}.csv");
        var link = Path.Combine(inputDirectory, "linked.csv");
        try
        {
            await File.WriteAllTextAsync(externalFile, "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion");
            File.CreateSymbolicLink(link, externalFile);

            await Assert.ThrowsAsync<ArgumentException>(() => CreateService().DetectAsync("linked.csv", CancellationToken.None));
        }
        finally
        {
            if (File.Exists(externalFile))
                File.Delete(externalFile);
        }
    }

    private ImportTemplateDetectionService CreateService(long maxFileSizeBytes = 1024 * 1024, int maxSpreadsheetCells = 100_000) =>
        new(inputDirectory, maxFileSizeBytes, inputDirectory, maxSpreadsheetCells: maxSpreadsheetCells);

        private static void CreateXlsx(string path)
        {
                using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
                WriteEntry("[Content_Types].xml", """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                            <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
                            <Default Extension="xml" ContentType="application/xml" />
                            <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
                            <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
                        </Types>
                        """);
                WriteEntry("_rels/.rels", """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                            <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
                        </Relationships>
                        """);
                WriteEntry("xl/workbook.xml", """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                            <sheets><sheet name="Movimientos" sheetId="1" r:id="rId1" /></sheets>
                        </workbook>
                        """);
                WriteEntry("xl/_rels/workbook.xml.rels", """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                            <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
                        </Relationships>
                        """);
                WriteEntry("xl/worksheets/sheet1.xml", """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                            <sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>Fecha</t></is></c><c r="B1" t="inlineStr"><is><t>Descripción</t></is></c><c r="C1" t="inlineStr"><is><t>Débito</t></is></c><c r="D1" t="inlineStr"><is><t>Crédito</t></is></c></row></sheetData>
                        </worksheet>
                        """);

                void WriteEntry(string entryName, string content)
                {
                        using var writer = new StreamWriter(archive.CreateEntry(entryName).Open());
                        writer.Write(content);
                }
        }

    public void Dispose()
    {
        if (Directory.Exists(inputDirectory))
            Directory.Delete(inputDirectory, recursive: true);
    }
}
