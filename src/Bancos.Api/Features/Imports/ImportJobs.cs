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
public sealed class ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ClassificationService classification, ILogger<ImportJobs> logger)
{
    public ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ILogger<ImportJobs> logger)
        : this(db, detector, bcrParser, financingParser, loanParser, new ClassificationService(db), logger) { }

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
            else if (template == ImportTemplates.BcrDebitCsvV1)
            {
                WriteStage(context, "Extracting and validating BCR debit movements.");
                var movements = bcrParser.Parse(await File.ReadAllTextAsync(import.TemporaryPath));
                var fingerprints = movements.Select(movement => CreateFingerprint(import.AccountAuxiliaryId, movement)).ToArray();
                var existing = await db.Transactions.Where(transaction => transaction.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(transaction.SourceFingerprint)).Select(transaction => transaction.SourceFingerprint).ToListAsync();
                foreach (var movement in movements.Zip(fingerprints))
                {
                    if (existing.Contains(movement.Second)) continue;
                    var transaction = new Transaction { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, BookingDate = movement.First.BookingDate, ExternalReference = movement.First.ExternalReference, SourceFingerprint = movement.Second, AmountCrc = movement.First.Credit - movement.First.Debit, OriginalAmount = movement.First.Credit - movement.First.Debit, DescriptionNormalized = ImportTemplateDetector.Normalize(movement.First.Description) };
                    await classification.ClassifyAsync(transaction);
                    db.Transactions.Add(transaction);
                }
                WriteStage(context, "Validated {0} movements and persisted the new fingerprints.", movements.Count);
            }
            else throw new InvalidDataException($"El extractor para '{template}' no está disponible.");
            import.Status = ImportStatus.Completed; import.ProcessedUtc = DateTime.UtcNow; await db.SaveChangesAsync(); WriteStage(context, "Import completed.");
        }
        catch (Exception exception) { import.Status = ImportStatus.Failed; import.FailureReason = exception.Message; await db.SaveChangesAsync(); throw; }
        finally { if (File.Exists(import.TemporaryPath)) File.Delete(import.TemporaryPath); }
    }

    private static void WriteStage(PerformContext? context, string message, params object[] arguments) => context?.WriteLine(message, arguments);
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedBankMovement movement)
    {
        var source = string.Join('|', accountAuxiliaryId, movement.BookingDate, movement.ExternalReference.Trim(), ImportTemplateDetector.Normalize(movement.Description), movement.Debit, movement.Credit, "CRC");
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
