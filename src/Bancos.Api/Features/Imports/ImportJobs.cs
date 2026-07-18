using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Parsing;
using Bancos.Api.Features.Classification;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bancos.Api.Features.Imports;
public sealed class ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, AccountMovementSpreadsheetParser spreadsheetParser, BacCreditFinancingXlsParser financingParser, CardStatementParser cardParser, CoopealianzaLoanPdfParser loanParser, ClassificationService classification, ILogger<ImportJobs> logger)
{
    public ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ClassificationService classification, ILogger<ImportJobs> logger)
        : this(db, detector, bcrParser, new AccountMovementSpreadsheetParser(), financingParser, new CardStatementParser(), loanParser, classification, logger) { }
    public ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, AccountMovementSpreadsheetParser spreadsheetParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ILogger<ImportJobs> logger)
        : this(db, detector, bcrParser, spreadsheetParser, financingParser, new CardStatementParser(), loanParser, new ClassificationService(db), logger) { }

    public async Task ProcessAsync(Guid importId, PerformContext? context)
    {
        var import = await db.Imports.SingleOrDefaultAsync(x => x.Id == importId) ?? throw new InvalidOperationException("Import was not found.");
        if (import.Status == ImportStatus.Completed) return;
        import.Status = ImportStatus.Processing; await db.SaveChangesAsync(); WriteStage(context, "Starting import."); logger.LogInformation("Processing import {ImportId}", importId);
        try
        {
            WriteStage(context, "Detecting template from content.");
            var detectedTemplate = detector.Detect(await File.ReadAllBytesAsync(import.TemporaryPath)).Template;
            var template = import.Template ?? detectedTemplate;
            if (ImportReviewTemplates.Get(template) is not { IsEnabled: true }) throw new InvalidDataException("No hay un extractor habilitado para este tipo de archivo.");
            import.Template = template;
            WriteStage(context, "Template detected: {0}", template);
            if (template == ImportTemplates.BacCreditFinancingXlsV1)
            {
                WriteStage(context, "Extracting and validating BAC credit financings.");
                var financings = financingParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                var fingerprints = financings.Select(financing => CreateFingerprint(import.AccountAuxiliaryId, financing)).ToArray();
                var existing = await db.CreditFinancings.Where(financing => financing.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(financing.SourceFingerprint)).Select(financing => financing.SourceFingerprint).ToListAsync();
                foreach (var financing in financings.Zip(fingerprints))
                {
                    if (existing.Contains(financing.Second)) continue;
                    db.CreditFinancings.Add(new CreditFinancing { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, FinancingDate = financing.First.FinancingDate, Concept = financing.First.Concept, Installments = financing.First.Installments, InstallmentAmount = financing.First.InstallmentAmount, InitialBalance = financing.First.InitialBalance, OutstandingBalance = financing.First.OutstandingBalance, SourceFingerprint = financing.Second });
                }
                WriteStage(context, "Validated {0} financings and persisted the new fingerprints.", financings.Count);
            }
            else if (template == ImportTemplates.CoopealianzaLoanPdfV1)
            {
                WriteStage(context, "Extracting and validating Coopealianza loan payments.");
                var loan = loanParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                var loanFingerprint = CreateFingerprint(import.AccountAuxiliaryId, loan);
                var statement = await db.LoanStatements.Include(x => x.Payments).SingleOrDefaultAsync(x => x.AccountAuxiliaryId == import.AccountAuxiliaryId && x.SourceFingerprint == loanFingerprint);
                if (statement is null)
                {
                    statement = new LoanStatement { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, OutstandingBalance = loan.OutstandingBalance, SourceFingerprint = loanFingerprint };
                    foreach (var payment in loan.Payments)
                        statement.Payments.Add(new LoanPayment { PaymentDate = payment.PaymentDate, Capital = payment.Capital, Interest = payment.Interest, LateFee = payment.LateFee, OtherCharges = payment.OtherCharges, Total = payment.Total, SourceFingerprint = CreateFingerprint(payment) });
                    db.LoanStatements.Add(statement);
                }
                WriteStage(context, "Validated balance and {0} loan payments; persisted new fingerprints.", loan.Payments.Count);
            }
            else if (template is ImportTemplates.BcrDebitCsvV1 or ImportTemplates.BcrDebitHtmlXlsV1)
            {
                var movements = template == ImportTemplates.BcrDebitCsvV1 ? bcrParser.Parse(await File.ReadAllTextAsync(import.TemporaryPath)) : spreadsheetParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                await PersistMovements(import, movements, classification);
                WriteStage(context, "Validated {0} movements and persisted the new fingerprints.", movements.Count);
            }
            else if (template is ImportTemplates.BacCreditCsvV1 or ImportTemplates.BacCreditOnlinePdfV1)
            {
                WriteStage(context, "Extracting and validating card statement movements.");
                var movements = cardParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                await PersistCardMovements(import, movements, classification);
                WriteStage(context, "Validated {0} card movements and persisted the new fingerprints.", movements.Count);
            }
            else throw new InvalidDataException($"El extractor para '{template}' no está disponible.");
            import.Status = ImportStatus.Completed; import.ProcessedUtc = DateTime.UtcNow; await db.SaveChangesAsync(); WriteStage(context, "Import completed.");
        }
        catch (Exception exception) { import.Status = ImportStatus.Failed; import.FailureReason = exception.Message; await db.SaveChangesAsync(); throw; }
        finally { if (File.Exists(import.TemporaryPath)) File.Delete(import.TemporaryPath); }
    }

    private static void WriteStage(PerformContext? context, string message, params object[] arguments) => context?.WriteLine(message, arguments);
    private async Task PersistCardMovements(Import import, IReadOnlyList<ParsedCardMovement> movements, ClassificationService classification)
    {
        var rateDates = movements.Where(movement => movement.OriginalCurrencyCode == "USD" && movement.AmountCrc is null).Select(movement => movement.BookingDate).Distinct().ToArray();
        var rates = rateDates.Length == 0 ? new Dictionary<DateOnly, decimal>() : await db.ExchangeRates
            .Where(rate => rate.CurrencyCode == "USD" && rateDates.Contains(rate.RateDate))
            .ToDictionaryAsync(rate => rate.RateDate, rate => rate.CrcPerUnit);
        var normalizedMovements = movements.Select(movement => movement.AmountCrc is not null ? movement : rates.TryGetValue(movement.BookingDate, out var rate)
            ? movement with { AmountCrc = movement.OriginalAmount * rate }
            : throw new InvalidDataException($"No existe tipo de cambio USD para la fecha {movement.BookingDate:yyyy-MM-dd}.")).ToArray();
        var fingerprints = normalizedMovements.Select(movement => CreateFingerprint(import.AccountAuxiliaryId, movement)).ToArray();
        var existing = await db.Transactions.Where(transaction => transaction.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(transaction.SourceFingerprint)).Select(transaction => transaction.SourceFingerprint).ToListAsync();
        foreach (var movement in normalizedMovements.Zip(fingerprints))
        {
            if (existing.Contains(movement.Second)) continue;
            var transaction = new Transaction
            {
                ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, BookingDate = movement.First.BookingDate,
                ExternalReference = movement.First.ExternalReference, SourceFingerprint = movement.Second, AmountCrc = movement.First.AmountCrc!.Value,
                OriginalAmount = movement.First.OriginalAmount, OriginalCurrencyCode = movement.First.OriginalCurrencyCode,
                ExchangeRate = movement.First.OriginalCurrencyCode == "USD" && movement.First.OriginalAmount != 0 ? movement.First.AmountCrc!.Value / movement.First.OriginalAmount : null,
                OperationType = movement.First.Operation switch { CardOperationKind.Payment => TransactionOperationType.CardPayment, CardOperationKind.Interest => TransactionOperationType.CardInterest, CardOperationKind.Charge => TransactionOperationType.CardCharge, _ => TransactionOperationType.CardPurchase },
                DescriptionNormalized = ImportTemplateDetector.Normalize(movement.First.Description)
            };
            await classification.ClassifyAsync(transaction);
            db.Transactions.Add(transaction);
        }
    }
    private async Task PersistMovements(Import import, IReadOnlyList<ParsedBankMovement> movements, ClassificationService classification)
    {
        var fingerprints = movements.Select(movement => CreateFingerprint(import.AccountAuxiliaryId, movement)).ToArray();
        var existing = await db.Transactions.Where(transaction => transaction.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(transaction.SourceFingerprint)).Select(transaction => transaction.SourceFingerprint).ToListAsync();
        foreach (var movement in movements.Zip(fingerprints))
        {
            if (existing.Contains(movement.Second)) continue;
            var transaction = new Transaction { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, BookingDate = movement.First.BookingDate, ExternalReference = movement.First.ExternalReference, SourceFingerprint = movement.Second, AmountCrc = movement.First.Credit - movement.First.Debit, OriginalAmount = movement.First.Credit - movement.First.Debit, DescriptionNormalized = ImportTemplateDetector.Normalize(movement.First.Description) };
            await classification.ClassifyAsync(transaction);
            db.Transactions.Add(transaction);
        }
    }
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedBankMovement movement)
    {
        var source = string.Join('|', accountAuxiliaryId, movement.BookingDate, movement.ExternalReference.Trim(), ImportTemplateDetector.Normalize(movement.Description), movement.Debit, movement.Credit, "CRC");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedCardMovement movement)
    {
        var source = string.Join('|', accountAuxiliaryId, movement.BookingDate, movement.ExternalReference.Trim(), ImportTemplateDetector.Normalize(movement.Description), movement.OriginalAmount, movement.OriginalCurrencyCode, movement.AmountCrc, movement.Operation);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedCreditFinancing financing)
    {
        var source = string.Join('|', accountAuxiliaryId, financing.FinancingDate, ImportTemplateDetector.Normalize(financing.Concept), financing.Installments.Trim(), financing.InstallmentAmount, financing.InitialBalance, financing.OutstandingBalance, "CRC");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedCoopealianzaLoan loan)
    {
        var source = string.Join('|', accountAuxiliaryId, loan.OutstandingBalance, string.Join(';', loan.Payments.Select(CreateFingerprint)), "CRC");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
    private static string CreateFingerprint(ParsedCoopealianzaLoanPayment payment)
    {
        var source = string.Join('|', payment.PaymentDate, payment.Capital, payment.Interest, payment.LateFee, payment.OtherCharges, payment.Total, "CRC");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
