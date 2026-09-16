using System.Security.Cryptography;
using System.Text;

namespace Bancos.Mcp.Features.Parsing;

internal static class FingerprintHelper
{
    public static string Compute(params string[] fields)
    {
        var source = string.Join('|', fields);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    public static string ForBankMovement(Guid bankAccountId, ParsedBankMovement m) =>
        Compute(bankAccountId.ToString(), m.BookingDate.ToString(), m.ExternalReference.Trim(), TextNormalizer.Normalize(m.Description), m.Debit.ToString(), m.Credit.ToString(), "CRC");

    public static string ForCardMovement(Guid bankAccountId, ParsedCardMovement m) =>
        Compute(bankAccountId.ToString(), m.BookingDate.ToString(), m.ExternalReference.Trim(), TextNormalizer.Normalize(m.Description), m.OriginalAmount.ToString(), m.OriginalCurrencyCode, m.AmountCrc?.ToString() ?? "", m.Operation.ToString());

    public static string ForCreditFinancing(Guid bankAccountId, ParsedCreditFinancing f) =>
        Compute(bankAccountId.ToString(), f.FinancingDate.ToString(), TextNormalizer.Normalize(f.Concept), f.Installments.Trim(), f.InstallmentAmount.ToString(), f.InitialBalance.ToString(), f.OutstandingBalance.ToString(), "CRC");

    public static string ForBacAccountStatement(Guid bankAccountId, ParsedBacAccountStatement s) =>
        Compute(bankAccountId.ToString(), s.CardNumberMasked, s.StatementDate.ToString(), s.MinimumPaymentCrc.ToString(), s.MinimumPaymentUsd.ToString(), s.CashPaymentCrc.ToString(), s.CashPaymentUsd.ToString());

    public static string ForBnCardStatement(Guid bankAccountId, ParsedBnCardStatement s) =>
        Compute(bankAccountId.ToString(), s.CardNumberMasked, s.StatementDate.ToString(), s.MinimumPaymentCrc.ToString(), s.MinimumPaymentUsd.ToString(), s.CashPaymentCrc.ToString(), s.CashPaymentUsd.ToString());

    public static string ForBnFinancing(Guid bankAccountId, DateOnly statementDate, ParsedBnFinancingLine f) =>
        Compute(bankAccountId.ToString(), TextNormalizer.Normalize(f.Origin), statementDate.ToString(), f.CurrentInstallmentNumber.ToString(), f.TotalInstallments.ToString(), f.OutstandingBalance.ToString(), f.CurrencyCode);

    public static string ForCoopealianzaLoan(Guid bankAccountId, ParsedCoopealianzaLoan loan) =>
        Compute(bankAccountId.ToString(), loan.OriginalAmount.ToString(), loan.TermMonths.ToString(), loan.StartDate.ToString(), "CRC");

    public static string ForLoanPayment(ParsedCoopealianzaLoanPayment p) =>
        Compute(p.PaymentDate.ToString(), p.Capital.ToString(), p.Interest.ToString(), p.LateFee.ToString(), p.OtherCharges.ToString(), p.Total.ToString(), "CRC");

    public static string ForLoanCuota(Guid bankAccountId, ParsedCoopealianzaLoanCuota c) =>
        Compute(bankAccountId.ToString(), c.CuotaNumber.ToString(), c.DueDate.ToString(), c.Capital.ToString(), c.Interest.ToString(), c.Total.ToString(), "CRC");
}
