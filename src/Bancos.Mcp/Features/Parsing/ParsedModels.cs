namespace Bancos.Mcp.Features.Parsing;

public sealed record ParsedBankMovement(DateOnly BookingDate, string ExternalReference, string Description, decimal Debit, decimal Credit);

public sealed record ParsedCardMovement(
    DateOnly BookingDate,
    string ExternalReference,
    string Description,
    decimal OriginalAmount,
    string OriginalCurrencyCode,
    decimal? AmountCrc,
    CardOperationKind Operation);

public enum CardOperationKind { Purchase, Payment, Interest, Charge }
public enum CardStatementContentKind { Movements, PaymentSummary, BalanceSnapshot }

public sealed record ParsedCardStatement(
    CardStatementContentKind ContentKind,
    IReadOnlyList<ParsedCardMovement> Movements)
{
    public bool RequiresManualReview => ContentKind is not CardStatementContentKind.Movements;
}

public sealed record ParsedCreditFinancing(DateOnly FinancingDate, string Concept, string Installments, decimal InstallmentAmount, decimal InitialBalance, decimal OutstandingBalance, string CurrencyCode);

public sealed record ParsedCoopealianzaLoanPayment(DateOnly PaymentDate, decimal Capital, decimal Interest, decimal LateFee, decimal OtherCharges, decimal Total);
public sealed record ParsedCoopealianzaLoanCuota(int CuotaNumber, DateOnly DueDate, decimal Balance, decimal Capital, decimal Interest, decimal LateFee, decimal OtherCharges, decimal Total, string Status);
public sealed record ParsedCoopealianzaLoan(
    decimal OriginalAmount, decimal InterestRate, int TermMonths, DateOnly StartDate,
    decimal OutstandingBalance,
    IReadOnlyList<ParsedCoopealianzaLoanPayment> Payments,
    IReadOnlyList<ParsedCoopealianzaLoanCuota> Cuotas);

public sealed record ParsedBacAccountStatement(
    string CardNumberMasked, string CardBrand, string LoyaltyPlan,
    DateOnly StatementDate, DateOnly PaymentDueDate,
    decimal MinimumPaymentCrc, decimal MinimumPaymentUsd,
    decimal CashPaymentCrc, decimal CashPaymentUsd);

public sealed record ParsedBnCardStatement(
    string CardNumberMasked,
    string CardBrand,
    string LoyaltyPlan,
    DateOnly StatementDate,
    DateOnly PaymentDueDate,
    decimal MinimumPaymentCrc,
    decimal MinimumPaymentUsd,
    decimal CashPaymentCrc,
    decimal CashPaymentUsd,
    IReadOnlyList<ParsedCardMovement> Movements,
    IReadOnlyList<ParsedBnFinancingLine> FinancingLines);

public sealed record ParsedBnFinancingLine(
    string Origin,
    string CurrencyCode,
    decimal OriginalAmount,
    decimal OutstandingBalance,
    decimal InstallmentAmount,
    int TotalInstallments,
    int CurrentInstallmentNumber,
    DateOnly StartDate,
    DateOnly EndDate);
