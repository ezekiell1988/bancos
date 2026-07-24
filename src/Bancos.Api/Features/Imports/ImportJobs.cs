using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Parsing;
using Bancos.Api.Features.Classification;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bancos.Api.Features.Imports;
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed class ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, AccountMovementSpreadsheetParser spreadsheetParser, BacCreditFinancingXlsParser financingParser, CardStatementParser cardParser, CoopealianzaLoanPdfParser loanParser, BacAccountStatementPdfParser accountStatementParser, BnCardStatementPdfParser bnParser, ClassificationService classification, IImportProgressReporter progress, ILogger<ImportJobs> logger)
{
    public ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ClassificationService classification, ILogger<ImportJobs> logger)
        : this(db, detector, bcrParser, new AccountMovementSpreadsheetParser(), financingParser, new CardStatementParser(), loanParser, new BacAccountStatementPdfParser(), new BnCardStatementPdfParser(), classification, NullImportProgressReporter.Instance, logger) { }
    public ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, AccountMovementSpreadsheetParser spreadsheetParser, BacCreditFinancingXlsParser financingParser, CoopealianzaLoanPdfParser loanParser, ILogger<ImportJobs> logger)
        : this(db, detector, bcrParser, spreadsheetParser, financingParser, new CardStatementParser(), loanParser, new BacAccountStatementPdfParser(), new BnCardStatementPdfParser(), new ClassificationService(db), NullImportProgressReporter.Instance, logger) { }

    public async Task ProcessAsync(Guid importId, PerformContext? context)
    {
        var import = await db.Imports.SingleOrDefaultAsync(x => x.Id == importId) ?? throw new InvalidOperationException("Import was not found.");
        import.Status = ImportStatus.Processing; import.FailureReason = null; await db.SaveChangesAsync();
        var attempt = await progress.BeginAttemptAsync(importId, context);
        WriteStage(context, "Starting import."); logger.LogInformation("Processing import {ImportId}", importId);
        try
        {
            await progress.ReportAsync(importId, attempt, ImportProgressStages.DetectingTemplate, 0, 0, 5);
            WriteStage(context, "Detecting template from content.");
            var detectedTemplate = detector.Detect(await File.ReadAllBytesAsync(import.TemporaryPath)).Template;
            var template = import.Template ?? detectedTemplate;
            if (ImportReviewTemplates.Get(template) is not { IsEnabled: true }) throw new InvalidDataException("No hay un extractor habilitado para este tipo de archivo.");
            import.Template = template;
            WriteStage(context, "Template detected: {0}", template);
            if (template == ImportTemplates.BacCreditFinancingXlsV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
                WriteStage(context, "Extracting and validating BAC credit financings.");
                var financings = financingParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                var rawConcepts = financings.Select(f => f.Concept.Trim()).ToArray();
                var dates = financings.Select(f => f.FinancingDate).ToArray();
                var existingFinancings = await db.CreditFinancings
                    .Where(f => f.AccountAuxiliaryId == import.AccountAuxiliaryId && dates.Contains(f.FinancingDate) && rawConcepts.Contains(f.Concept))
                    .ToListAsync();
                var fingerprints = financings.Select(financing => CreateFingerprint(import.AccountAuxiliaryId, financing)).ToArray();
                foreach (var (parsed, fingerprint) in financings.Zip(fingerprints))
                {
                    var match = existingFinancings.FirstOrDefault(f => f.FinancingDate == parsed.FinancingDate && f.Concept == parsed.Concept.Trim());
                    if (match is not null)
                    {
                        match.ImportId = import.Id;
                        match.Installments = parsed.Installments;
                        match.InstallmentAmount = parsed.InstallmentAmount;
                        match.OutstandingBalance = parsed.OutstandingBalance;
                        match.CurrencyCode = parsed.CurrencyCode;
                        match.SourceFingerprint = fingerprint;
                    }
                    else
                    {
                        db.CreditFinancings.Add(new CreditFinancing { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, FinancingDate = parsed.FinancingDate, Concept = parsed.Concept, Installments = parsed.Installments, InstallmentAmount = parsed.InstallmentAmount, InitialBalance = parsed.InitialBalance, OutstandingBalance = parsed.OutstandingBalance, CurrencyCode = parsed.CurrencyCode, SourceFingerprint = fingerprint });
                    }
                }
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, financings.Count, financings.Count, 70);
                WriteStage(context, "Validated {0} financings and persisted the new fingerprints.", financings.Count);
            }
            else if (template == ImportTemplates.BacAccountStatementPdfV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
                WriteStage(context, "Extracting BAC consolidated account statement.");
                var statements = accountStatementParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                var cardNumbers = statements.Select(s => s.CardNumberMasked).ToArray();
                var statementDates = statements.Select(s => s.StatementDate).ToArray();
                var existing = await db.CardStatements
                    .Where(s => s.AccountAuxiliaryId == import.AccountAuxiliaryId && cardNumbers.Contains(s.CardNumberMasked) && statementDates.Contains(s.StatementDate))
                    .ToListAsync();
                foreach (var parsed in statements)
                {
                    var fingerprint = BacAccountStatementPdfParser.CreateFingerprint(import.AccountAuxiliaryId, parsed);
                    var match = existing.FirstOrDefault(s => s.CardNumberMasked == parsed.CardNumberMasked && s.StatementDate == parsed.StatementDate);
                    if (match is not null)
                    {
                        match.ImportId = import.Id; match.LoyaltyPlan = parsed.LoyaltyPlan; match.PaymentDueDate = parsed.PaymentDueDate;
                        match.MinimumPaymentCrc = parsed.MinimumPaymentCrc; match.MinimumPaymentUsd = parsed.MinimumPaymentUsd;
                        match.CashPaymentCrc = parsed.CashPaymentCrc; match.CashPaymentUsd = parsed.CashPaymentUsd;
                        match.SourceFingerprint = fingerprint;
                    }
                    else
                    {
                        db.CardStatements.Add(new CardStatement { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, CardNumberMasked = parsed.CardNumberMasked, CardBrand = parsed.CardBrand, LoyaltyPlan = parsed.LoyaltyPlan, StatementDate = parsed.StatementDate, PaymentDueDate = parsed.PaymentDueDate, MinimumPaymentCrc = parsed.MinimumPaymentCrc, MinimumPaymentUsd = parsed.MinimumPaymentUsd, CashPaymentCrc = parsed.CashPaymentCrc, CashPaymentUsd = parsed.CashPaymentUsd, SourceFingerprint = fingerprint });
                    }
                }
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, statements.Count, statements.Count, 70);
                WriteStage(context, "Processed {0} card statements.", statements.Count);
            }
            else if (template == ImportTemplates.CoopealianzaLoanPdfV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
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
                    try { await db.SaveChangesAsync(); }
                    catch (DbUpdateException)
                    {
                        db.ChangeTracker.Clear();
                        if (!await db.LoanStatements.AnyAsync(x => x.AccountAuxiliaryId == import.AccountAuxiliaryId && x.SourceFingerprint == loanFingerprint))
                            throw;
                        // Concurrent job won the race — statement already exists; treat as already imported.
                        db.Imports.Attach(import);
                    }
                }
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, loan.Payments.Count, loan.Payments.Count, 70);
                WriteStage(context, "Validated balance and {0} loan payments; persisted new fingerprints.", loan.Payments.Count);
            }
            else if (template is ImportTemplates.BcrDebitCsvV1 or ImportTemplates.BcrDebitHtmlXlsV1 or ImportTemplates.BankAccountMovementsXlsV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
                var movements = template == ImportTemplates.BcrDebitCsvV1 ? bcrParser.Parse(await File.ReadAllTextAsync(import.TemporaryPath)) : spreadsheetParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                await PersistMovements(import, movements, classification, progress, attempt);
                WriteStage(context, "Validated {0} movements and persisted the new fingerprints.", movements.Count);
            }
            else if (template is ImportTemplates.BacCreditCsvV1 or ImportTemplates.BacCreditOnlinePdfV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
                WriteStage(context, "Extracting and validating card statement movements.");
                var statement = cardParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                if (statement.RequiresManualReview) throw new InvalidDataException(statement.ManualReviewReason);
                await PersistCardMovements(import, statement.Movements, classification, progress, attempt);
                WriteStage(context, "Validated {0} card movements and persisted the new fingerprints.", statement.Movements.Count);
            }
            else if (template == ImportTemplates.BnCardStatementPdfV1)
            {
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, 0, 10);
                WriteStage(context, "Extracting Banco Nacional card statement.");
                var bnStatement = bnParser.Parse(await File.ReadAllBytesAsync(import.TemporaryPath));
                var fingerprint = BnCardStatementPdfParser.CreateFingerprint(import.AccountAuxiliaryId, bnStatement);
                var existingStatement = await db.CardStatements
                    .FirstOrDefaultAsync(s => s.AccountAuxiliaryId == import.AccountAuxiliaryId && s.CardNumberMasked == bnStatement.CardNumberMasked && s.StatementDate == bnStatement.StatementDate);
                if (existingStatement is not null)
                {
                    existingStatement.ImportId = import.Id; existingStatement.LoyaltyPlan = bnStatement.LoyaltyPlan; existingStatement.PaymentDueDate = bnStatement.PaymentDueDate;
                    existingStatement.MinimumPaymentCrc = bnStatement.MinimumPaymentCrc; existingStatement.MinimumPaymentUsd = bnStatement.MinimumPaymentUsd;
                    existingStatement.CashPaymentCrc = bnStatement.CashPaymentCrc; existingStatement.CashPaymentUsd = bnStatement.CashPaymentUsd;
                    existingStatement.SourceFingerprint = fingerprint;
                }
                else
                {
                    db.CardStatements.Add(new CardStatement { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, CardNumberMasked = bnStatement.CardNumberMasked, CardBrand = bnStatement.CardBrand, LoyaltyPlan = bnStatement.LoyaltyPlan, StatementDate = bnStatement.StatementDate, PaymentDueDate = bnStatement.PaymentDueDate, MinimumPaymentCrc = bnStatement.MinimumPaymentCrc, MinimumPaymentUsd = bnStatement.MinimumPaymentUsd, CashPaymentCrc = bnStatement.CashPaymentCrc, CashPaymentUsd = bnStatement.CashPaymentUsd, SourceFingerprint = fingerprint });
                }
                WriteStage(context, "Card statement upserted. Processing {0} movements.", bnStatement.Movements.Count);
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, 0, bnStatement.Movements.Count, 30);
                if (bnStatement.Movements.Count > 0)
                    await PersistCardMovements(import, bnStatement.Movements, classification, progress, attempt);
                // Upsert financing lines
                if (bnStatement.FinancingLines.Count > 0)
                {
                    var financingFingerprints = bnStatement.FinancingLines
                        .Select(f => BnCardStatementPdfParser.CreateFinancingFingerprint(import.AccountAuxiliaryId, bnStatement.StatementDate, f)).ToArray();
                    var existingFinancings = await db.CreditFinancings
                        .Where(f => f.AccountAuxiliaryId == import.AccountAuxiliaryId && financingFingerprints.Contains(f.SourceFingerprint))
                        .Select(f => f.SourceFingerprint).ToListAsync();
                    foreach (var (line, fp) in bnStatement.FinancingLines.Zip(financingFingerprints))
                    {
                        if (!existingFinancings.Contains(fp))
                            db.CreditFinancings.Add(new CreditFinancing { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, FinancingDate = bnStatement.StatementDate, Concept = line.Origin, Installments = $"{line.CurrentInstallmentNumber}/{line.TotalInstallments}", InstallmentAmount = line.InstallmentAmount, InitialBalance = line.OriginalAmount, OutstandingBalance = line.OutstandingBalance, CurrencyCode = line.CurrencyCode, SourceFingerprint = fp });
                    }
                    WriteStage(context, "Processed {0} financing lines.", bnStatement.FinancingLines.Count);
                }
                await progress.ReportAsync(importId, attempt, ImportProgressStages.Extracting, bnStatement.Movements.Count, bnStatement.Movements.Count, 70);
            }
            else throw new InvalidDataException($"El extractor para '{template}' no está disponible.");
            await progress.ReportAsync(importId, attempt, ImportProgressStages.Persisting, 0, 0, 95);
            import.Status = ImportStatus.Completed; import.ProcessedUtc = DateTime.UtcNow; await db.SaveChangesAsync();
            await progress.ReportAsync(importId, attempt, ImportProgressStages.Completed, 0, 0, 100, ImportStatus.Completed);
            WriteStage(context, "Import completed.");
        }
        catch (InvalidDataException exception)
        {
            db.ChangeTracker.Clear(); db.Imports.Attach(import);
            import.Status = ImportStatus.Failed;
            import.FailureReason = exception.Message;
            await db.SaveChangesAsync();
            await progress.ReportAsync(importId, attempt, ImportProgressStages.Failed, 0, 0, 0, ImportStatus.Failed);
            // Validation failures are final import outcomes: keeping the source permits review,
            // while completing the Hangfire invocation prevents a retry of unchanged content.
            WriteStage(context, "Import validation failed: {0}", exception.Message);
        }
        catch (Exception)
        {
            db.ChangeTracker.Clear(); db.Imports.Attach(import);
            import.Status = ImportStatus.Failed;
            import.FailureReason = "La importación no pudo procesarse.";
            await db.SaveChangesAsync();
            await progress.ReportAsync(importId, attempt, ImportProgressStages.Failed, 0, 0, 0, ImportStatus.Failed);
            throw;
        }
        finally { if (import.Status == ImportStatus.Completed && File.Exists(import.TemporaryPath)) File.Delete(import.TemporaryPath); }
    }

    private static void WriteStage(PerformContext? context, string message, params object[] arguments) => context?.WriteLine(message, arguments);
    private async Task PersistCardMovements(Import import, IReadOnlyList<ParsedCardMovement> movements, ClassificationService classification, IImportProgressReporter progress, int attempt)
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
        var index = 0;
        foreach (var movement in normalizedMovements.Zip(fingerprints))
        {
            index++;
            if (!existing.Contains(movement.Second))
            {
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
            await ReportClassificationProgress(import.Id, attempt, index, normalizedMovements.Length, progress);
        }
    }
    private async Task PersistMovements(Import import, IReadOnlyList<ParsedBankMovement> movements, ClassificationService classification, IImportProgressReporter progress, int attempt)
    {
        var fingerprints = movements.Select(movement => CreateFingerprint(import.AccountAuxiliaryId, movement)).ToArray();
        var existing = await db.Transactions.Where(transaction => transaction.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(transaction.SourceFingerprint)).Select(transaction => transaction.SourceFingerprint).ToListAsync();
        var index = 0;
        foreach (var movement in movements.Zip(fingerprints))
        {
            index++;
            if (!existing.Contains(movement.Second))
            {
                var transaction = new Transaction { ImportId = import.Id, AccountAuxiliaryId = import.AccountAuxiliaryId, BookingDate = movement.First.BookingDate, ExternalReference = movement.First.ExternalReference, SourceFingerprint = movement.Second, AmountCrc = movement.First.Credit - movement.First.Debit, OriginalAmount = movement.First.Credit - movement.First.Debit, DescriptionNormalized = ImportTemplateDetector.Normalize(movement.First.Description) };
                await classification.ClassifyAsync(transaction);
                db.Transactions.Add(transaction);
            }
            await ReportClassificationProgress(import.Id, attempt, index, movements.Count, progress);
        }
    }
    private static Task ReportClassificationProgress(Guid importId, int attempt, int current, int total, IImportProgressReporter progress) =>
        progress.ReportAsync(importId, attempt, ImportProgressStages.Classifying, current, total, total == 0 ? 90 : 10 + current * 80 / total);
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
        var source = string.Join('|', accountAuxiliaryId, financing.FinancingDate, ImportTemplateDetector.Normalize(financing.Concept), financing.Installments.Trim(), financing.InstallmentAmount, financing.InitialBalance, financing.OutstandingBalance, financing.CurrencyCode);
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
