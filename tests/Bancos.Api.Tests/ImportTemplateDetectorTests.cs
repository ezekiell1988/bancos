using System.Text;
using System.IO.Compression;
using System.Reflection;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Imports;
using Bancos.Api.Features.Parsing;
using Bancos.Api.Features.Classification;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class ImportTemplateDetectorTests
{
    [Fact]
    public void Import_jobs_fail_without_automatic_retries()
    {
        var retry = typeof(ImportJobs).GetCustomAttribute<AutomaticRetryAttribute>();

        Assert.NotNull(retry);
        Assert.Equal(0, retry.Attempts);
        Assert.Equal(AttemptsExceededAction.Fail, retry.OnAttemptsExceeded);
    }

    [Fact]
    public void Keeps_a_stable_identity_for_repeated_zip_paths()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var content in new[] { "first", "second" })
            {
                var entry = archive.CreateEntry("statements/movements.csv");
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: false);
                writer.Write(content);
            }
        }

        var entries = ZipImportReader.Read("fixture.zip", stream.ToArray());

        Assert.Collection(entries,
            first => { Assert.Equal(0, first.EntryIndex); Assert.Equal("statements/movements.csv", first.Path); Assert.Equal("first", Encoding.UTF8.GetString(first.Content)); },
            second => { Assert.Equal(1, second.EntryIndex); Assert.Equal("statements/movements.csv", second.Path); Assert.Equal("second", Encoding.UTF8.GetString(second.Content)); });
    }

    [Fact]
    public void Resolves_the_first_matching_entry_index_without_throwing()
    {
        var first = new ImportSource(0, "first.csv", Encoding.UTF8.GetBytes("first"));
        var duplicate = new ImportSource(0, "second.csv", Encoding.UTF8.GetBytes("second"));

        var source = ImportsModule.FindSource([first, duplicate], "first.csv", 0);

        Assert.Same(first, source);
    }

    [Fact]
    public void Resolves_the_first_matching_path_without_an_entry_index()
    {
        var first = new ImportSource(0, "movements.csv", Encoding.UTF8.GetBytes("first"));
        var duplicate = new ImportSource(1, "movements.csv", Encoding.UTF8.GetBytes("second"));

        var source = ImportsModule.FindSource([first, duplicate], "movements.csv", null);

        Assert.Same(first, source);
    }

    [Fact]
    public void Parses_card_csv_operations_and_preserves_usd_with_crc_equivalent()
    {
        var csv = "Date,Description,Operation,Reference,Amount,Currency,Amount CRC\n2026-07-01,Compra de prueba,Compra,REF-1,10.00,USD,5200.00\n2026-07-02,Pago de prueba,Pago,REF-2,2000.00,CRC,2000.00\n2026-07-03,Interés de prueba,Interés,REF-3,25.00,CRC,25.00\n2026-07-04,Cargo de prueba,Cargo,REF-4,15.00,CRC,15.00";

        var movements = new CardStatementParser().ParseCsv(csv);

        Assert.Collection(movements,
            purchase => { Assert.Equal(CardOperationKind.Purchase, purchase.Operation); Assert.Equal("USD", purchase.OriginalCurrencyCode); Assert.Equal(10m, purchase.OriginalAmount); Assert.Equal(5200m, purchase.AmountCrc); },
            payment => Assert.Equal(CardOperationKind.Payment, payment.Operation),
            interest => Assert.Equal(CardOperationKind.Interest, interest.Operation),
            charge => Assert.Equal(CardOperationKind.Charge, charge.Operation));
    }

    [Fact]
    public void Keeps_usd_card_movement_without_crc_equivalent_for_daily_rate_resolution()
    {
        var csv = "Date,Description,Amount,Currency\n2026-07-01,Compra de prueba,10.00,USD";

        var movement = Assert.Single(new CardStatementParser().ParseCsv(csv));
        Assert.Null(movement.AmountCrc);
    }

    [Fact]
    public void Detects_and_parses_bac_card_payment_summary_without_cardholder_data()
    {
        var csv = "Product,Name,Date,Minimum payment/due date,Minimum payment/ Local Amount,Minimum Payment / Dollars Amount,Cash payment/Due date,Cash payment/ Local amount,Cash payment / Dollar amount\nCard product,Ignored,2026-07-01,2026-07-10,0,0,2026-07-15,2000,0";

        var detection = new ImportTemplateDetector().Detect(Encoding.UTF8.GetBytes(csv));
        var movement = Assert.Single(new CardStatementParser().ParseCsv(csv));

        Assert.Equal(ImportTemplates.BacCreditCsvV1, detection.Template);
        Assert.Equal(CardOperationKind.Payment, movement.Operation);
        Assert.Equal("Pago de tarjeta", movement.Description);
        Assert.Equal(2000m, movement.AmountCrc);
    }

    [Fact]
    public void Detects_and_parses_bac_card_payment_summary_without_product_header()
    {
        var csv = "Identifier,Name,Date,Minimum payment/due date,Minimum payment/ Local Amount,Minimum Payment / Dollars Amount,Cash payment/Due date,Cash payment/ Local amount,Cash payment / Dollar amount\nA-1,Ignored,2026-07-01,2026-07-10,0,0,2026-07-15,2000,0";

        var detection = new ImportTemplateDetector().Detect(Encoding.UTF8.GetBytes(csv));
        var movement = Assert.Single(new CardStatementParser().ParseCsv(csv));

        Assert.Equal(ImportTemplates.BacCreditCsvV1, detection.Template);
        Assert.Equal(CardOperationKind.Payment, movement.Operation);
        Assert.Equal(2000m, movement.AmountCrc);
    }

    [Fact]
    public void Detects_binary_account_movement_spreadsheet_signature()
    {
        var detection = ImportTemplateDetector.Detect("Fecha Referencia Descripción Débito Crédito Saldo", "xls");

        Assert.Equal(ImportTemplates.BankAccountMovementsXlsV1, detection.Template);
    }

    [Fact]
    public async Task Persists_card_operation_and_currency_amounts()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auxiliary = new AccountAuxiliary { Name = "Fixture card", AccountId = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var path = Path.GetTempFileName();
        var import = new Import { FileName = "card.csv", TemporaryPath = path, ContentHash = "card-fixture-hash", AccountAuxiliaryId = auxiliary.Id, Template = ImportTemplates.BacCreditCsvV1 };
        db.AccountAuxiliaries.Add(auxiliary); db.Imports.Add(import); await db.SaveChangesAsync();
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), new BacCreditFinancingXlsParser(), new CoopealianzaLoanPdfParser(), new ClassificationService(db), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllTextAsync(path, "Date,Description,Operation,Reference,Amount,Currency,Amount CRC\n2026-07-01,Compra de prueba,Compra,REF-1,10.00,USD,5200.00");
            await job.ProcessAsync(import.Id, null);

            var transaction = Assert.Single(await db.Transactions.ToListAsync());
            Assert.Equal(TransactionOperationType.CardPurchase, transaction.OperationType);
            Assert.Equal("USD", transaction.OriginalCurrencyCode);
            Assert.Equal(10m, transaction.OriginalAmount);
            Assert.Equal(5200m, transaction.AmountCrc);
            Assert.False(File.Exists(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

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
    public async Task Learns_a_safe_structure_and_reuses_it_before_static_detection()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var patterns = new ImportTemplatePatternService(db);
        var confirmed = Encoding.UTF8.GetBytes("Nombre;Referencia;Fecha;Monto\nPersona privada;ABC-001;2026-07-18;1.234,50");
        var future = Encoding.UTF8.GetBytes("Nombre;Referencia;Fecha;Monto\nOtra persona;XYZ-999;2026-08-21;9.876,00");

        await patterns.LearnAsync(confirmed, ImportTemplates.BcrDebitCsvV1, CancellationToken.None);
        var detection = await patterns.DetectAsync(future, CancellationToken.None);
        var pattern = Assert.Single(await db.ImportTemplatePatterns.ToListAsync());

        Assert.Equal(ImportTemplates.BcrDebitCsvV1, detection.Template);
        Assert.Equal(["patrón confirmado localmente"], detection.Evidence);
        Assert.DoesNotContain("Persona", pattern.SignatureHash, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1.234,50", pattern.SignatureHash, StringComparison.Ordinal);
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

    [Theory]
    [InlineData("1,453.00", 1453)]
    [InlineData("1.453,00", 1453)]
    public void Parses_bcr_export_amounts_with_thousands_separators(string value, decimal expected)
    {
        var csv = $"oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;{value};;Movimiento de prueba";

        var movement = Assert.Single(new BcrDebitCsvParser().Parse(csv));

        Assert.Equal(expected, movement.Debit);
    }

    [Fact]
    public void Rejects_bcr_movement_with_two_directions()
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n01;18/07/2026;ref-1;10,00;5,00;Movimiento de prueba";

        Assert.Throws<InvalidDataException>(() => new BcrDebitCsvParser().Parse(csv));
    }

    [Fact]
    public void Ignores_only_the_anonymized_bcr_trailing_summary_signature()
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion;\n"
            + "01;17/07/2026;ref-1;10,00;;Movimiento de prueba;90,00\n"
            + "01;18/07/2026;resumen;10,00;5,00;;";

        var movement = Assert.Single(new BcrDebitCsvParser().Parse(csv));

        Assert.Equal(10m, movement.Debit);
        Assert.Equal(0m, movement.Credit);
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
    public void Detects_and_parses_anonymized_bcr_html_xls_movements()
    {
        var html = """
            <html><body><h1>Banco de Costa Rica</h1><p>Movimientos por rango de fechas</p>
            <table>
              <tr><th>Fecha movimiento</th><th>Número documento</th><th>Descripción</th><th>Débito</th><th>Crédito</th></tr>
              <tr><td>18/07/2026</td><td>REF-1</td><td>Movimiento anonimizado</td><td>10,00</td><td></td></tr>
            </table></body></html>
            """;
        var content = Encoding.UTF8.GetBytes(html);

        var detection = new ImportTemplateDetector().Detect(content);
        var movement = Assert.Single(new AccountMovementSpreadsheetParser().Parse(content));

        Assert.Equal(ImportTemplates.BcrDebitHtmlXlsV1, detection.Template);
        Assert.Equal(new DateOnly(2026, 7, 18), movement.BookingDate);
        Assert.Equal("REF-1", movement.ExternalReference);
        Assert.Equal("Movimiento anonimizado", movement.Description);
        Assert.Equal(10m, movement.Debit);
        Assert.Equal(0m, movement.Credit);
    }

    [Fact]
    public void Parses_anonymized_account_html_with_accounting_date_headers()
    {
        var html = """
            <html><body><table>
              <tr><th>Fecha contable</th><th>Fecha transacción</th><th>Documento</th><th>Descripción</th><th>Débitos</th><th>Créditos</th></tr>
              <tr><td>18/07/2026</td><td>17/07/2026</td><td>REF-1</td><td>Movimiento anonimizado</td><td></td><td>25,00</td></tr>
            </table></body></html>
            """;

        var movement = Assert.Single(new AccountMovementSpreadsheetParser().Parse(Encoding.UTF8.GetBytes(html)));

        Assert.Equal(new DateOnly(2026, 7, 18), movement.BookingDate);
        Assert.Equal(0m, movement.Debit);
        Assert.Equal(25m, movement.Credit);
    }

    [Fact]
    public void Detects_and_parses_anonymized_coopealianza_loan_pdf_fixture()
    {
        var content = CoopealianzaLoanPdfFixture.Create();

        var detection = new ImportTemplateDetector().Detect(content);
        var loan = new CoopealianzaLoanPdfParser().Parse(content);

        Assert.Equal(ImportTemplates.CoopealianzaLoanPdfV1, detection.Template);
        Assert.Equal(1250m, loan.OutstandingBalance);
        var payment = Assert.Single(loan.Payments);
        Assert.Equal(new DateOnly(2026, 7, 1), payment.PaymentDate);
        Assert.Equal(125m, payment.Total);
    }

    [Fact]
    public void Rejects_coopealianza_payment_that_does_not_reconcile()
    {
        var content = CoopealianzaLoanPdfFixture.Create("01/07/2026 100,00 20,00 0,00 5,00 124,00");

        Assert.Throws<InvalidDataException>(() => new CoopealianzaLoanPdfParser().Parse(content));
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
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), new BacCreditFinancingXlsParser(), new CoopealianzaLoanPdfParser(), new ClassificationService(db), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllTextAsync(path, csv); await job.ProcessAsync(import.Id, null);
            await File.WriteAllTextAsync(path, csv); await job.ProcessAsync(import.Id, null);
            Assert.Single(await db.Transactions.ToListAsync());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Detects_and_parses_anonymized_bac_financing_xls_fixture()
    {
        var content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bac-credit-financing-anonymized.xls"));
        var detection = new ImportTemplateDetector().Detect(content);
        var financings = new BacCreditFinancingXlsParser().Parse(content);

        Assert.Equal(ImportTemplates.BacCreditFinancingXlsV1, detection.Template);
        var financing = Assert.Single(financings);
        Assert.Equal(new DateOnly(2026, 7, 18), financing.FinancingDate);
        Assert.Equal("Financiamiento anonimizado", financing.Concept);
        Assert.Equal("12", financing.Installments);
        Assert.Equal(12500m, financing.InstallmentAmount);
        Assert.Equal(100000m, financing.InitialBalance);
        Assert.Equal(87500m, financing.OutstandingBalance);
    }

    [Fact]
    public async Task Reprocessing_financing_xls_does_not_duplicate_structured_financings()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auxiliary = new AccountAuxiliary { Name = "Fixture auxiliary", AccountId = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var path = Path.GetTempFileName();
        var content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bac-credit-financing-anonymized.xls"));
        var import = new Import { FileName = "fixture.xls", TemporaryPath = path, ContentHash = "financing-fixture-hash", AccountAuxiliaryId = auxiliary.Id };
        db.AccountAuxiliaries.Add(auxiliary); db.Imports.Add(import); await db.SaveChangesAsync();
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), new BacCreditFinancingXlsParser(), new CoopealianzaLoanPdfParser(), new ClassificationService(db), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllBytesAsync(path, content); await job.ProcessAsync(import.Id, null);
            await File.WriteAllBytesAsync(path, content); await job.ProcessAsync(import.Id, null);
            Assert.Single(await db.CreditFinancings.ToListAsync());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Invalid_financing_data_marks_import_as_failed()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auxiliary = new AccountAuxiliary { Name = "Fixture auxiliary", AccountId = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var path = Path.GetTempFileName();
        var import = new Import { FileName = "invalid.xls", TemporaryPath = path, ContentHash = "invalid-financing-fixture-hash", AccountAuxiliaryId = auxiliary.Id };
        db.AccountAuxiliaries.Add(auxiliary); db.Imports.Add(import); await db.SaveChangesAsync();
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), new BacCreditFinancingXlsParser(), new CoopealianzaLoanPdfParser(), new ClassificationService(db), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllBytesAsync(path, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bac-credit-financing-invalid.xls")));
            await Assert.ThrowsAnyAsync<Exception>(() => job.ProcessAsync(import.Id, null));
            Assert.Equal(ImportStatus.Failed, (await db.Imports.SingleAsync()).Status);
            Assert.True(File.Exists(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Reprocessing_coopealianza_loan_pdf_does_not_duplicate_balance_or_payments()
    {
        await using var db = new BancosDbContext(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auxiliary = new AccountAuxiliary { Name = "Fixture auxiliary", AccountId = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();
        var content = CoopealianzaLoanPdfFixture.Create();
        var first = new Import { FileName = "fixture.pdf", TemporaryPath = firstPath, ContentHash = "loan-fixture-one", AccountAuxiliaryId = auxiliary.Id };
        var second = new Import { FileName = "fixture.pdf", TemporaryPath = secondPath, ContentHash = "loan-fixture-two", AccountAuxiliaryId = auxiliary.Id };
        db.AccountAuxiliaries.Add(auxiliary); db.Imports.AddRange(first, second); await db.SaveChangesAsync();
        var job = new ImportJobs(db, new ImportTemplateDetector(), new BcrDebitCsvParser(), new BacCreditFinancingXlsParser(), new CoopealianzaLoanPdfParser(), new ClassificationService(db), NullLogger<ImportJobs>.Instance);
        try
        {
            await File.WriteAllBytesAsync(firstPath, content); await job.ProcessAsync(first.Id, null);
            await File.WriteAllBytesAsync(secondPath, content); await job.ProcessAsync(second.Id, null);

            Assert.Single(await db.LoanStatements.ToListAsync());
            Assert.Single(await db.LoanPayments.ToListAsync());
        }
        finally { if (File.Exists(firstPath)) File.Delete(firstPath); if (File.Exists(secondPath)) File.Delete(secondPath); }
    }
}

internal static class CoopealianzaLoanPdfFixture
{
    public static byte[] Create(string? paymentLine = null)
    {
        var lines = new[]
        {
            "Ver detalles del prestamo",
            "Saldo actual 1.250,00",
            "Fecha Capital Interes Mora Otros Total",
            paymentLine ?? "01/07/2026 100,00 20,00 0,00 5,00 125,00"
        };
        var textOperations = string.Join("\n", lines.Select(line => $"({Escape(line)}) Tj\nT*"));
        var stream = $"BT\n/F1 12 Tf\n14 TL\n72 720 Td\n{textOperations}\nET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n"); writer.Flush();
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n"); writer.Flush();
        }
        var xref = output.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); writer.Flush();
        return output.ToArray();
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
