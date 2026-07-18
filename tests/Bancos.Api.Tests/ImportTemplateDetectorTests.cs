using System.Text;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Imports;
using Bancos.Api.Features.Parsing;
using Bancos.Api.Features.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class ImportTemplateDetectorTests
{
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
