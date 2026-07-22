using Bancos.Mcp.Catalog;
using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Parsing;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.FileProcessing;

[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed class ImportFileJob(
    McpCatalogDbContext db,
    BcrDebitCsvParser bcrParser,
    AccountMovementSpreadsheetParser spreadsheetParser,
    BacCreditFinancingXlsParser financingParser,
    CardStatementParser cardParser,
    CoopealianzaLoanPdfParser loanParser,
    BacAccountStatementPdfParser accountStatementParser,
    BnCardStatementPdfParser bnParser,
    ILogger<ImportFileJob> logger)
{
    public async Task ExecuteAsync(string filePath, string parserKey, Guid bankAccountId, PerformContext? context)
    {
        context?.WriteLine("Iniciando procesamiento: {0} con parser {1}", Path.GetFileName(filePath), parserKey);
        logger.LogInformation("Processing {File} with parser {ParserKey} for account {AccountId}", filePath, parserKey, bankAccountId);

        try
        {
            switch (parserKey)
            {
                case "bcr-debit-csv":
                    await ProcessBankMovements(bankAccountId, bcrParser.Parse(await File.ReadAllTextAsync(filePath)), context);
                    break;
                case "bcr-debit-html":
                case "bank-account-movements-xls":
                    await ProcessBankMovements(bankAccountId, spreadsheetParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-credit-financing-xls":
                    await ProcessCreditFinancings(bankAccountId, financingParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-credit-csv":
                case "bac-credit-online-pdf":
                    var statement = cardParser.Parse(await File.ReadAllBytesAsync(filePath));
                    if (statement.RequiresManualReview) throw new InvalidDataException("El archivo requiere revisión manual y no contiene movimientos procesables.");
                    await ProcessCardMovements(bankAccountId, statement.Movements, context);
                    break;
                case "coopealianza-loan-pdf":
                    await ProcessLoan(bankAccountId, loanParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-account-statement-pdf":
                    await ProcessBacAccountStatements(bankAccountId, accountStatementParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bn-card-statement-pdf":
                    await ProcessBnCardStatement(bankAccountId, bnParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                default:
                    throw new InvalidDataException($"No hay parser disponible para '{parserKey}'.");
            }

            await db.SaveChangesAsync();
            context?.WriteLine("Procesamiento completado.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            context?.WriteLine("Duplicado detectado, archivo ya fue procesado previamente. Omitido.");
            logger.LogInformation("Duplicate detected for {File}, skipping.", filePath);
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            context?.WriteLine("Error: {0}", ex.Message);
            throw;
        }
    }

    private async Task ProcessBankMovements(Guid bankAccountId, IReadOnlyList<ParsedBankMovement> movements, PerformContext? context)
    {
        var fingerprints = movements.Select(m => FingerprintHelper.ForBankMovement(bankAccountId, m)).ToArray();
        var existing = await db.Transactions
            .Where(t => t.BankAccountId == bankAccountId && fingerprints.Contains(t.SourceFingerprint))
            .Select(t => t.SourceFingerprint).ToListAsync();

        var inserted = 0;
        foreach (var (movement, fingerprint) in movements.Zip(fingerprints))
        {
            if (existing.Contains(fingerprint)) continue;
            db.Transactions.Add(new Transaction
            {
                BankAccountId = bankAccountId,
                TransactionDate = movement.BookingDate,
                ReferenceNumber = movement.ExternalReference,
                Description = TextNormalizer.Normalize(movement.Description),
                CurrencyCode = "CRC",
                Amount = movement.Credit - movement.Debit,
                AmountCrc = movement.Credit - movement.Debit,
                OperationType = "General",
                SourceFingerprint = fingerprint
            });
            inserted++;
        }
        context?.WriteLine("Movimientos: {0} insertados, {1} duplicados omitidos.", inserted, movements.Count - inserted);
    }

    private async Task ProcessCardMovements(Guid bankAccountId, IReadOnlyList<ParsedCardMovement> movements, PerformContext? context)
    {
        var rateDates = movements.Where(m => m.OriginalCurrencyCode == "USD" && m.AmountCrc is null).Select(m => m.BookingDate).Distinct().ToArray();
        var rates = rateDates.Length == 0 ? new Dictionary<DateOnly, decimal>() : await db.ExchangeRates
            .Where(r => r.CurrencyCode == "USD" && rateDates.Contains(r.RateDate))
            .ToDictionaryAsync(r => r.RateDate, r => r.CrcPerUnit);

        var normalized = movements.Select(m => m.AmountCrc is not null ? m : rates.TryGetValue(m.BookingDate, out var rate)
            ? m with { AmountCrc = m.OriginalAmount * rate }
            : throw new InvalidDataException($"No existe tipo de cambio USD para la fecha {m.BookingDate:yyyy-MM-dd}.")).ToArray();

        var fingerprints = normalized.Select(m => FingerprintHelper.ForCardMovement(bankAccountId, m)).ToArray();
        var existing = await db.Transactions
            .Where(t => t.BankAccountId == bankAccountId && fingerprints.Contains(t.SourceFingerprint))
            .Select(t => t.SourceFingerprint).ToListAsync();

        var inserted = 0;
        foreach (var (movement, fingerprint) in normalized.Zip(fingerprints))
        {
            if (existing.Contains(fingerprint)) continue;
            db.Transactions.Add(new Transaction
            {
                BankAccountId = bankAccountId,
                TransactionDate = movement.BookingDate,
                ReferenceNumber = movement.ExternalReference,
                Description = TextNormalizer.Normalize(movement.Description),
                CurrencyCode = movement.OriginalCurrencyCode,
                Amount = movement.OriginalAmount,
                AmountCrc = movement.AmountCrc!.Value,
                ExchangeRate = movement.OriginalCurrencyCode == "USD" && movement.OriginalAmount != 0 ? movement.AmountCrc!.Value / movement.OriginalAmount : null,
                OperationType = movement.Operation switch
                {
                    CardOperationKind.Payment => "CardPayment",
                    CardOperationKind.Interest => "CardInterest",
                    CardOperationKind.Charge => "CardCharge",
                    _ => "CardPurchase"
                },
                SourceFingerprint = fingerprint
            });
            inserted++;
        }
        context?.WriteLine("Movimientos de tarjeta: {0} insertados, {1} duplicados omitidos.", inserted, movements.Count - inserted);
    }

    private async Task ProcessCreditFinancings(Guid bankAccountId, IReadOnlyList<ParsedCreditFinancing> financings, PerformContext? context)
    {
        var fingerprints = financings.Select(f => FingerprintHelper.ForCreditFinancing(bankAccountId, f)).ToArray();
        var dates = financings.Select(f => f.FinancingDate).ToArray();
        var rawConcepts = financings.Select(f => f.Concept.Trim()).ToArray();
        var existingFinancings = await db.CardFinancings
            .Where(f => f.BankAccountId == bankAccountId && dates.Contains(f.FinancingDate) && rawConcepts.Contains(f.Concept))
            .ToListAsync();

        foreach (var (parsed, fingerprint) in financings.Zip(fingerprints))
        {
            var match = existingFinancings.FirstOrDefault(f => f.FinancingDate == parsed.FinancingDate && f.Concept == parsed.Concept.Trim());
            if (match is not null)
            {
                match.Installments = parsed.Installments;
                match.InstallmentAmount = parsed.InstallmentAmount;
                match.OutstandingBalance = parsed.OutstandingBalance;
                match.CurrencyCode = parsed.CurrencyCode;
                match.SourceFingerprint = fingerprint;
                match.UpdatedAt = CostaRicaTime.Now;
            }
            else
            {
                db.CardFinancings.Add(new CardFinancing
                {
                    BankAccountId = bankAccountId,
                    FinancingDate = parsed.FinancingDate,
                    Concept = parsed.Concept.Trim(),
                    CurrencyCode = parsed.CurrencyCode,
                    InitialBalance = parsed.InitialBalance,
                    OutstandingBalance = parsed.OutstandingBalance,
                    Installments = parsed.Installments,
                    InstallmentAmount = parsed.InstallmentAmount,
                    Status = "Active",
                    SourceFingerprint = fingerprint
                });
            }
        }
        context?.WriteLine("Financiamientos: {0} procesados.", financings.Count);
    }

    private async Task ProcessLoan(Guid bankAccountId, ParsedCoopealianzaLoan loan, PerformContext? context)
    {
        var existing = await db.LoanStatements
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.BankAccountId == bankAccountId);

        if (existing is null)
        {
            existing = new LoanStatement
            {
                BankAccountId = bankAccountId,
                StatementDate = DateOnly.FromDateTime(DateTime.Today),
                CurrencyCode = "CRC",
                OriginalLoanAmount = loan.OriginalAmount,
                InterestRate = loan.InterestRate,
                TermMonths = loan.TermMonths,
                StartDate = loan.StartDate,
                OutstandingBalance = loan.OutstandingBalance,
                SourceFingerprint = FingerprintHelper.ForCoopealianzaLoan(bankAccountId, loan)
            };
            db.LoanStatements.Add(existing);
        }
        else
        {
            existing.OriginalLoanAmount = loan.OriginalAmount;
            existing.InterestRate = loan.InterestRate;
            existing.TermMonths = loan.TermMonths;
            existing.StartDate = loan.StartDate;
            existing.OutstandingBalance = loan.OutstandingBalance;
            existing.SourceFingerprint = FingerprintHelper.ForCoopealianzaLoan(bankAccountId, loan);
            existing.UpdatedAt = CostaRicaTime.Now;
        }

        var existingFingerprints = existing.Payments.Select(p => p.SourceFingerprint).ToHashSet();
        var inserted = 0;

        foreach (var c in loan.Cuotas)
        {
            var fp = FingerprintHelper.ForLoanCuota(bankAccountId, c);
            if (existingFingerprints.Contains(fp)) continue;
            existing.Payments.Add(new LoanPayment
            {
                PaymentDate = c.DueDate,
                Capital = c.Capital,
                Interest = c.Interest,
                LateFee = c.LateFee,
                OtherCharges = c.OtherCharges,
                Total = c.Total,
                Balance = c.Balance,
                SourceFingerprint = fp
            });
            inserted++;
        }
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff12 = today.AddMonths(12);
        var vigentes = loan.Cuotas.Where(c => c.Status == "Vigente").ToArray();

        var nextCuota = vigentes.Where(c => c.DueDate >= today).OrderBy(c => c.DueDate).FirstOrDefault();
        existing.NextMonthCapital = nextCuota?.Capital;
        existing.NextMonthInterest = nextCuota?.Interest;
        existing.NextMonthTotal = nextCuota?.Total;

        var currentPortion = vigentes.Where(c => c.DueDate >= today && c.DueDate <= cutoff12).ToArray();
        existing.CurrentPortionCapital = currentPortion.Sum(c => c.Capital);
        existing.CurrentPortionInterest = currentPortion.Sum(c => c.Interest);
        existing.CurrentPortionTotal = currentPortion.Sum(c => c.Total);

        var longTerm = vigentes.Where(c => c.DueDate > cutoff12).ToArray();
        existing.LongTermCapital = longTerm.Sum(c => c.Capital);
        existing.LongTermInterest = longTerm.Sum(c => c.Interest);
        existing.LongTermTotal = longTerm.Sum(c => c.Total);

        context?.WriteLine("Préstamo: ₡{0:N2} original, saldo ₡{1:N2}, {2} cuotas en archivo, {3} nuevas insertadas.", loan.OriginalAmount, loan.OutstandingBalance, loan.Cuotas.Count, inserted);
        context?.WriteLine("Porción corriente: ₡{0:N2} capital + ₡{1:N2} interés = ₡{2:N2}. Largo plazo: ₡{3:N2} capital + ₡{4:N2} interés = ₡{5:N2}.",
            existing.CurrentPortionCapital, existing.CurrentPortionInterest, existing.CurrentPortionTotal,
            existing.LongTermCapital, existing.LongTermInterest, existing.LongTermTotal);
    }

    private async Task ProcessBacAccountStatements(Guid bankAccountId, IReadOnlyList<ParsedBacAccountStatement> statements, PerformContext? context)
    {
        var statementDates = statements.Select(s => s.StatementDate).ToArray();
        var existing = await db.CardStatements
            .Where(s => s.BankAccountId == bankAccountId && statementDates.Contains(s.StatementDate))
            .ToListAsync();

        foreach (var parsed in statements)
        {
            var fingerprint = FingerprintHelper.ForBacAccountStatement(bankAccountId, parsed);
            var match = existing.FirstOrDefault(s => s.StatementDate == parsed.StatementDate);
            if (match is not null)
            {
                match.MinimumPaymentCrc = parsed.MinimumPaymentCrc;
                match.MinimumPaymentUsd = parsed.MinimumPaymentUsd;
                match.CashPaymentCrc = parsed.CashPaymentCrc;
                match.CashPaymentUsd = parsed.CashPaymentUsd;
                match.SourceFingerprint = fingerprint;
                match.UpdatedAt = CostaRicaTime.Now;
            }
            else
            {
                db.CardStatements.Add(new CardStatement
                {
                    BankAccountId = bankAccountId,
                    StatementDate = parsed.StatementDate,
                    PeriodLabel = $"{parsed.StatementDate:yyyy-MM}",
                    MinimumPaymentDueDate = parsed.PaymentDueDate,
                    CashPaymentDueDate = parsed.PaymentDueDate,
                    MinimumPaymentCrc = parsed.MinimumPaymentCrc,
                    MinimumPaymentUsd = parsed.MinimumPaymentUsd,
                    CashPaymentCrc = parsed.CashPaymentCrc,
                    CashPaymentUsd = parsed.CashPaymentUsd,
                    SourceFingerprint = fingerprint
                });
            }
        }
        context?.WriteLine("Estados de cuenta BAC: {0} procesados.", statements.Count);
    }

    private async Task ProcessBnCardStatement(Guid bankAccountId, ParsedBnCardStatement bn, PerformContext? context)
    {
        var fingerprint = FingerprintHelper.ForBnCardStatement(bankAccountId, bn);
        var existingStatement = await db.CardStatements
            .FirstOrDefaultAsync(s => s.BankAccountId == bankAccountId && s.StatementDate == bn.StatementDate);

        if (existingStatement is not null)
        {
            existingStatement.MinimumPaymentCrc = bn.MinimumPaymentCrc;
            existingStatement.MinimumPaymentUsd = bn.MinimumPaymentUsd;
            existingStatement.CashPaymentCrc = bn.CashPaymentCrc;
            existingStatement.CashPaymentUsd = bn.CashPaymentUsd;
            existingStatement.SourceFingerprint = fingerprint;
            existingStatement.UpdatedAt = CostaRicaTime.Now;
        }
        else
        {
            db.CardStatements.Add(new CardStatement
            {
                BankAccountId = bankAccountId,
                StatementDate = bn.StatementDate,
                PeriodLabel = $"{bn.StatementDate:yyyy-MM}",
                MinimumPaymentDueDate = bn.PaymentDueDate,
                CashPaymentDueDate = bn.PaymentDueDate,
                MinimumPaymentCrc = bn.MinimumPaymentCrc,
                MinimumPaymentUsd = bn.MinimumPaymentUsd,
                CashPaymentCrc = bn.CashPaymentCrc,
                CashPaymentUsd = bn.CashPaymentUsd,
                SourceFingerprint = fingerprint
            });
        }

        if (bn.Movements.Count > 0)
            await ProcessCardMovements(bankAccountId, bn.Movements, context);

        if (bn.FinancingLines.Count > 0)
        {
            var financingFingerprints = bn.FinancingLines
                .Select(f => FingerprintHelper.ForBnFinancing(bankAccountId, bn.StatementDate, f)).ToArray();
            var existingFinancings = await db.CardFinancings
                .Where(f => f.BankAccountId == bankAccountId && financingFingerprints.Contains(f.SourceFingerprint))
                .Select(f => f.SourceFingerprint).ToListAsync();

            foreach (var (line, fp) in bn.FinancingLines.Zip(financingFingerprints))
            {
                if (existingFinancings.Contains(fp)) continue;
                db.CardFinancings.Add(new CardFinancing
                {
                    BankAccountId = bankAccountId,
                    FinancingDate = bn.StatementDate,
                    Concept = line.Origin,
                    CurrencyCode = line.CurrencyCode,
                    InitialBalance = line.OriginalAmount,
                    OutstandingBalance = line.OutstandingBalance,
                    Installments = $"{line.CurrentInstallmentNumber}/{line.TotalInstallments}",
                    InstallmentAmount = line.InstallmentAmount,
                    Status = "Active",
                    SourceFingerprint = fp
                });
            }
            context?.WriteLine("Financiamientos BN: {0} procesados.", bn.FinancingLines.Count);
        }
    }
}
