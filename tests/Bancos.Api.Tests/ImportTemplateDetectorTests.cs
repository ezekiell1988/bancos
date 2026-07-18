using System.Text;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Imports;
using Bancos.Api.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class ImportTemplateDetectorTests
{
    [Fact]
    public void Detects_bcr_debit_csv_from_content_not_filename()
    {
        var content = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;10,00;;Movimiento de prueba";
        var result = new ImportTemplateDetector().Detect(Encoding.UTF8.GetBytes(content));
        Assert.Equal(ImportTemplates.BcrDebitCsvV1, result.Template);
        Assert.Equal("csv", result.ContentKind);
    }

    [Fact]
    public void Returns_unknown_for_an_ambiguous_or_unrecognized_signature()
    {
        var result = new ImportTemplateDetector().Detect(Encoding.UTF8.GetBytes("un contenido sin encabezados bancarios documentados"));
        Assert.Equal(ImportTemplates.Unknown, result.Template);
    }

    [Fact]
    public void Parses_and_validates_one_direction_per_bcr_movement()
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;10,00;;Movimiento de prueba";
        var movements = new BcrDebitCsvParser().Parse(csv);
        Assert.Single(movements);
        Assert.Equal(10m, movements[0].Debit);
        Assert.Equal(0m, movements[0].Credit);
    }

    [Fact]
    public void Rejects_bcr_movement_with_two_directions()
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;10,00;5,00;Movimiento de prueba";
        Assert.Throws<InvalidDataException>(() => new BcrDebitCsvParser().Parse(csv));
    }

    [Fact]
    public void Rejects_bcr_file_when_optional_balance_columns_do_not_reconcile()
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion;saldoInicial;saldoFinal\n01;18/07/2026;ref-1;10,00;;Movimiento de prueba;100,00;95,00";
        Assert.Throws<InvalidDataException>(() => new BcrDebitCsvParser().Parse(csv));
    }

    [Fact]
    public void Recognizes_pdf_and_html_signatures_from_extracted_content()
    {
        var pdf = ImportTemplateDetector.Detect("Tarjeta de crédito Saldo en colones Saldo en dólares Pago de tarjeta al día", "pdf");
        var html = ImportTemplateDetector.Detect("Banco de Costa Rica Movimientos por rango de fechas", "html");
        Assert.Equal(ImportTemplates.BacCreditOnlinePdfV1, pdf.Template);
        Assert.Equal(ImportTemplates.BcrDebitHtmlXlsV1, html.Template);
    }

    [Fact]
    public async Task Reprocessing_the_same_content_does_not_duplicate_movements()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auxiliary = new AccountAuxiliary { Name = "Fixture auxiliary", AccountId = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var path = Path.GetTempFileName();
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;10,00;;Movimiento de prueba";
        var import = new Import { FileName = "fixture.csv", TemporaryPath = path, ContentHash = "fixture-hash", AccountAuxiliaryId = auxiliary.Id };
        db.AccountAuxiliaries.Add(auxiliary); db.Imports.Add(import); await db.SaveChangesAsync();
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllTextAsync(path, csv); await job.ProcessAsync(import.Id, null);
            await File.WriteAllTextAsync(path, csv); await job.ProcessAsync(import.Id, null);
            Assert.Single(await db.Transactions.ToListAsync());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
