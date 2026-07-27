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
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ExecuteAsync(string filePath, string parserKey, Guid bankAccountId, Guid? usdBankAccountId, PerformContext? context)
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
                case "bn-debit-csv":
                    await ProcessBankMovementsUsd(bankAccountId, bcrParser.Parse(await File.ReadAllTextAsync(filePath)), context);
                    break;
                case "bn-debit-csv-crc":
                    await ProcessBankMovements(bankAccountId, bcrParser.Parse(await File.ReadAllTextAsync(filePath)), context);
                    break;
                case "bcr-debit-html":
                case "bank-account-movements-xls":
                    await ProcessBankMovements(bankAccountId, spreadsheetParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-credit-financing-xls":
                    await ProcessCreditFinancings(bankAccountId, usdBankAccountId, financingParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-credit-csv":
                {
                    var s = cardParser.Parse(await File.ReadAllBytesAsync(filePath));
                    if (s.RequiresManualReview) throw new InvalidDataException("El archivo requiere revisión manual y no contiene movimientos procesables.");
                    await ProcessCardMovements(bankAccountId, usdBankAccountId, s.Movements, useBankSign: true, context);
                    break;
                }
                case "bac-credit-online-pdf":
                {
                    var s = cardParser.Parse(await File.ReadAllBytesAsync(filePath));
                    if (s.RequiresManualReview) throw new InvalidDataException("El archivo requiere revisión manual y no contiene movimientos procesables.");
                    await ProcessCardMovements(bankAccountId, null, s.Movements, useBankSign: true, context);
                    break;
                }
                case "coopealianza-loan-pdf":
                    await ProcessLoan(bankAccountId, loanParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bac-account-statement-pdf":
                    await ProcessBacAccountStatements(bankAccountId, accountStatementParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
                    break;
                case "bn-card-statement-pdf":
                    await ProcessBnCardStatement(bankAccountId, usdBankAccountId, bnParser.Parse(await File.ReadAllBytesAsync(filePath)), context);
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

    private async Task ProcessBankMovementsUsd(Guid bankAccountId, IReadOnlyList<ParsedBankMovement> movements, PerformContext? context)
    {
        var allRates = await db.ExchangeRates
            .Where(r => r.CurrencyCode == "USD")
            .OrderByDescending(r => r.RateDate)
            .Select(r => new { r.RateDate, r.CrcPerUnit })
            .ToListAsync();
        if (allRates.Count == 0)
            throw new InvalidDataException("No existe ningún tipo de cambio USD disponible.");

        var fingerprints = movements.Select(m => FingerprintHelper.ForBankMovement(bankAccountId, m)).ToArray();
        var existing = await db.Transactions
            .Where(t => t.BankAccountId == bankAccountId && fingerprints.Contains(t.SourceFingerprint))
            .Select(t => t.SourceFingerprint).ToListAsync();

        var movementDates = movements.Select(m => m.BookingDate).Distinct().ToArray();
        var periods = await db.Periods
            .Where(p => movementDates.Any(d => p.StartDate <= d && d <= p.EndDate))
            .ToListAsync();

        var inserted = 0;
        foreach (var (movement, fingerprint) in movements.Zip(fingerprints))
        {
            if (existing.Contains(fingerprint)) continue;
            var netUsd = movement.Credit - movement.Debit;
            var rate = (allRates.FirstOrDefault(r => r.RateDate <= movement.BookingDate) ?? allRates[^1]).CrcPerUnit;
            var period = periods.FirstOrDefault(p => p.StartDate <= movement.BookingDate && movement.BookingDate <= p.EndDate);
            db.Transactions.Add(new Transaction
            {
                BankAccountId = bankAccountId,
                PeriodId = period?.Id,
                TransactionDate = movement.BookingDate,
                ReferenceNumber = movement.ExternalReference,
                Description = TextNormalizer.Normalize(movement.Description),
                CurrencyCode = "USD",
                Amount = netUsd,
                AmountCrc = netUsd * rate,
                ExchangeRate = rate,
                OperationType = "purchase",
                SourceFingerprint = fingerprint
            });
            inserted++;
        }
        context?.WriteLine("Movimientos USD: {0} insertados, {1} duplicados omitidos.", inserted, movements.Count - inserted);
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
                OperationType = "purchase",
                SourceFingerprint = fingerprint
            });
            inserted++;
        }
        context?.WriteLine("Movimientos: {0} insertados, {1} duplicados omitidos.", inserted, movements.Count - inserted);
    }

    private async Task<IReadOnlyList<Transaction>> ProcessCardMovements(
        Guid crcAccountId,
        Guid? usdAccountId,
        IReadOnlyList<ParsedCardMovement> movements,
        bool useBankSign,
        PerformContext? context)
    {
        // Resolve amountCrc for USD movements that don't have it yet
        var needsRate = movements.Where(m => m.OriginalCurrencyCode == "USD" && m.AmountCrc is null).ToArray();
        Dictionary<DateOnly, decimal> rates = [];
        if (needsRate.Length > 0)
        {
            var allRates = await db.ExchangeRates
                .Where(r => r.CurrencyCode == "USD")
                .OrderByDescending(r => r.RateDate)
                .Select(r => new { r.RateDate, r.CrcPerUnit })
                .ToListAsync();
            if (allRates.Count == 0)
                throw new InvalidDataException("No existe ningún tipo de cambio USD disponible.");
            foreach (var m in needsRate)
                rates[m.BookingDate] = (allRates.FirstOrDefault(r => r.RateDate <= m.BookingDate) ?? allRates[^1]).CrcPerUnit;
        }

        var normalized = movements
            .Select(m => m.AmountCrc is not null ? m : m with { AmountCrc = m.OriginalAmount * rates[m.BookingDate] })
            .ToArray();

        // Route each movement to its account: USD → usdAccountId (if provided), CRC → crcAccountId
        Guid AccountFor(ParsedCardMovement m) =>
            m.OriginalCurrencyCode == "USD" && usdAccountId.HasValue ? usdAccountId.Value : crcAccountId;

        var fingerprints = normalized.Select(m => FingerprintHelper.ForCardMovement(AccountFor(m), m)).ToArray();

        var allAccountIds = new[] { crcAccountId }.Concat(usdAccountId.HasValue ? [usdAccountId.Value] : Array.Empty<Guid>()).ToArray();
        var existing = await db.Transactions
            .Where(t => allAccountIds.Contains(t.BankAccountId) && fingerprints.Contains(t.SourceFingerprint))
            .ToListAsync();
        var existingByFingerprint = existing.ToDictionary(t => t.SourceFingerprint);

        var movementDates = normalized.Select(m => m.BookingDate).Distinct().ToArray();
        var firstMovementDate = movementDates.Min();
        var lastMovementDate = movementDates.Max();
        var periods = await db.Periods
            .Where(p => p.EndDate >= firstMovementDate && p.StartDate <= lastMovementDate)
            .ToListAsync();

        var inserted = 0;
        var updated = 0;
        var processed = new List<Transaction>(normalized.Length);
        foreach (var (movement, fingerprint) in normalized.Zip(fingerprints))
        {
            if (existingByFingerprint.TryGetValue(fingerprint, out var existing2))
            {
                existing2.UpdatedAt = CostaRicaTime.Now;
                processed.Add(existing2);
                updated++;
                continue;
            }

            var accountId = AccountFor(movement);
            var period = periods.FirstOrDefault(p => p.StartDate <= movement.BookingDate && movement.BookingDate <= p.EndDate);
            var (description, place) = SplitDescriptionPlace(movement.Description);

            // Sign convention: bank shows positive = debit (money out), negative = credit (money in).
            // We invert: expense = negative Amount, income/payment = positive Amount.
            decimal amount, amountCrc;
            string operationType;
            if (useBankSign)
            {
                amount = -movement.OriginalAmount;
                amountCrc = -movement.AmountCrc!.Value;
                operationType = movement.Operation switch
                {
                    CardOperationKind.Payment => "payment",
                    CardOperationKind.Interest => "interest",
                    CardOperationKind.Charge => "other-charge",
                    _ => amount >= 0 ? "payment" : "purchase"
                };
            }
            else
            {
                var abs = Math.Abs(movement.OriginalAmount);
                var absCrc = Math.Abs(movement.AmountCrc!.Value);
                var isPayment = movement.Operation == CardOperationKind.Payment;
                amount = isPayment ? abs : -abs;
                amountCrc = isPayment ? absCrc : -absCrc;
                operationType = movement.Operation switch
                {
                    CardOperationKind.Payment => "payment",
                    CardOperationKind.Interest => "interest",
                    CardOperationKind.Charge => "other-charge",
                    _ => "purchase"
                };
            }

            var absAmt = Math.Abs(movement.OriginalAmount);
            var absAmtCrc = Math.Abs(movement.AmountCrc!.Value);
            var norm = TextNormalizer.Normalize(description);
            var transaction = new Transaction
            {
                BankAccountId = accountId,
                PeriodId = period?.Id,
                TransactionDate = movement.BookingDate,
                ReferenceNumber = movement.ExternalReference,
                Description = norm[..Math.Min(200, norm.Length)],
                Place = string.IsNullOrWhiteSpace(place) ? null : place[..Math.Min(120, place.Length)],
                CurrencyCode = movement.OriginalCurrencyCode,
                Amount = amount,
                AmountCrc = amountCrc,
                ExchangeRate = movement.OriginalCurrencyCode == "USD" && absAmt != 0 ? absAmtCrc / absAmt : 1m,
                OperationType = operationType,
                SourceFingerprint = fingerprint
            };
            db.Transactions.Add(transaction);
            existingByFingerprint[fingerprint] = transaction;
            processed.Add(transaction);
            inserted++;
        }
        context?.WriteLine("Movimientos de tarjeta: {0} insertados, {1} actualizados.", inserted, updated);
        return processed;
    }

    private static readonly System.Text.RegularExpressions.Regex PlacePattern =
        new(@"\\([^\\]+)\\([A-Z]{1,3})\s*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static (string Description, string? Place) SplitDescriptionPlace(string raw)
    {
        var match = PlacePattern.Match(raw);
        if (!match.Success) return (raw.Trim(), null);
        var place = $"{match.Groups[1].Value.Trim()}, {match.Groups[2].Value.Trim()}";
        var description = raw[..match.Index].Trim().TrimEnd('\\').Trim();
        return (description, place);
    }

    private async Task ProcessCreditFinancings(Guid crcAccountId, Guid? usdAccountId, IReadOnlyList<ParsedCreditFinancing> financings, PerformContext? context)
    {
        var groups = financings.GroupBy(f => f.CurrencyCode == "USD" && usdAccountId.HasValue ? usdAccountId.Value : crcAccountId);
        var total = 0;
        foreach (var group in groups)
        {
            await SaveCreditFinancings(group.Key, group.ToList());
            total += group.Count();
        }
        context?.WriteLine("Financiamientos: {0} procesados.", total);
    }

    private async Task SaveCreditFinancings(Guid accountId, IReadOnlyList<ParsedCreditFinancing> financings)
    {
        var fingerprints = financings.Select(f => FingerprintHelper.ForCreditFinancing(accountId, f)).ToArray();
        var dates = financings.Select(f => f.FinancingDate).ToArray();
        var rawConcepts = financings.Select(f => f.Concept.Trim()).ToArray();
        var existing = await db.CardFinancings
            .Where(f => f.BankAccountId == accountId && dates.Contains(f.FinancingDate) && rawConcepts.Contains(f.Concept))
            .ToListAsync();

        foreach (var (parsed, fingerprint) in financings.Zip(fingerprints))
        {
            var match = existing.FirstOrDefault(f => f.FinancingDate == parsed.FinancingDate && f.Concept == parsed.Concept.Trim());
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
                    BankAccountId = accountId,
                    FinancingDate = parsed.FinancingDate,
                    Concept = parsed.Concept.Trim(),
                    CurrencyCode = parsed.CurrencyCode,
                    InitialBalance = parsed.InitialBalance,
                    OutstandingBalance = parsed.OutstandingBalance,
                    Installments = parsed.Installments,
                    InstallmentAmount = parsed.InstallmentAmount,
                    Status = "active",
                    SourceFingerprint = fingerprint
                });
            }
        }
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

        var paymentsByInstallment = existing.Payments.ToDictionary(p => p.InstallmentNumber);
        var inserted = 0;
        var updated = 0;

        foreach (var c in loan.Cuotas)
        {
            var fp = FingerprintHelper.ForLoanCuota(bankAccountId, c);
            if (paymentsByInstallment.TryGetValue(c.CuotaNumber, out var payment))
            {
                payment.PaymentDate = c.DueDate;
                payment.Capital = c.Capital;
                payment.Interest = c.Interest;
                payment.LateFee = c.LateFee;
                payment.OtherCharges = c.OtherCharges;
                payment.Total = c.Total;
                payment.Balance = c.Balance;
                payment.Status = c.Status;
                payment.SourceFingerprint = fp;
                payment.UpdatedAt = CostaRicaTime.Now;
                updated++;
            }
            else
            {
                existing.Payments.Add(new LoanPayment
                {
                    InstallmentNumber = c.CuotaNumber,
                    PaymentDate = c.DueDate,
                    Capital = c.Capital,
                    Interest = c.Interest,
                    LateFee = c.LateFee,
                    OtherCharges = c.OtherCharges,
                    Total = c.Total,
                    Balance = c.Balance,
                    Status = c.Status,
                    SourceFingerprint = fp
                });
                inserted++;
            }
        }
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff12 = today.AddMonths(12);
        var pendingPayments = existing.Payments
            .Where(p => p.Status == "Vigente" && p.PaymentDate >= today)
            .OrderBy(p => p.PaymentDate)
            .ToArray();

        var nextCuota = pendingPayments.FirstOrDefault();
        existing.NextMonthCapital = nextCuota?.Capital;
        existing.NextMonthInterest = nextCuota?.Interest;
        existing.NextMonthTotal = nextCuota?.Total;

        var currentPortion = pendingPayments.Where(p => p.PaymentDate <= cutoff12).ToArray();
        existing.CurrentPortionCapital = currentPortion.Sum(c => c.Capital);
        existing.CurrentPortionInterest = currentPortion.Sum(c => c.Interest);
        existing.CurrentPortionTotal = currentPortion.Sum(c => c.Total);

        var longTerm = pendingPayments.Where(p => p.PaymentDate > cutoff12).ToArray();
        existing.LongTermCapital = longTerm.Sum(c => c.Capital);
        existing.LongTermInterest = longTerm.Sum(c => c.Interest);
        existing.LongTermTotal = longTerm.Sum(c => c.Total);

        context?.WriteLine("Préstamo procesado: {0} cuotas en archivo, {1} nuevas y {2} actualizadas.", loan.Cuotas.Count, inserted, updated);
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

    internal async Task ProcessBnCardStatement(
        Guid bankAccountId,
        Guid? usdBankAccountId,
        ParsedBnCardStatement bn,
        PerformContext? context)
    {
        var fingerprint = FingerprintHelper.ForBnCardStatement(bankAccountId, bn);
        var existingStatement = await db.CardStatements
            .Include(statement => statement.Lines)
            .FirstOrDefaultAsync(s => s.BankAccountId == bankAccountId && s.StatementDate == bn.StatementDate);

        CardStatement statement;
        if (existingStatement is not null)
        {
            statement = existingStatement;
            existingStatement.MinimumPaymentCrc = bn.MinimumPaymentCrc;
            existingStatement.MinimumPaymentUsd = bn.MinimumPaymentUsd;
            existingStatement.CashPaymentCrc = bn.CashPaymentCrc;
            existingStatement.CashPaymentUsd = bn.CashPaymentUsd;
            existingStatement.PreviousBalanceCrc = bn.PreviousBalanceCrc;
            existingStatement.PreviousBalanceUsd = bn.PreviousBalanceUsd;
            existingStatement.CurrentBalanceCrc = bn.CurrentBalanceCrc;
            existingStatement.CurrentBalanceUsd = bn.CurrentBalanceUsd;
            existingStatement.SourceFingerprint = fingerprint;
            existingStatement.UpdatedAt = CostaRicaTime.Now;
        }
        else
        {
            statement = new CardStatement
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
                PreviousBalanceCrc = bn.PreviousBalanceCrc,
                PreviousBalanceUsd = bn.PreviousBalanceUsd,
                CurrentBalanceCrc = bn.CurrentBalanceCrc,
                CurrentBalanceUsd = bn.CurrentBalanceUsd,
                SourceFingerprint = fingerprint
            };
            db.CardStatements.Add(statement);
        }

        if (bn.Movements.Count > 0)
        {
            var transactions = await ProcessCardMovements(
                bankAccountId, usdBankAccountId, bn.Movements, useBankSign: false, context);
            var linkedTransactionIds = statement.Lines.Select(line => line.TransactionId).ToHashSet();
            foreach (var transaction in transactions
                         .DistinctBy(transaction => transaction.SourceFingerprint)
                         .Where(transaction => transaction.Id == Guid.Empty || !linkedTransactionIds.Contains(transaction.Id)))
            {
                statement.Lines.Add(new CardStatementLine { Transaction = transaction });
                if (transaction.Id != Guid.Empty) linkedTransactionIds.Add(transaction.Id);
            }
        }

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
