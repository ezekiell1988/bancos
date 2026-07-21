using Bancos.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Loans;

public static class LoansModule
{
    public static IEndpointRouteBuilder MapLoansEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/loans").WithTags("Loans")
           .MapGet("/", GetLoans);
        return app;
    }

    private static async Task<Ok<LoansResponse>> GetLoans(BancosDbContext db, CancellationToken ct)
    {
        var financings = await db.CreditFinancings
            .AsNoTracking()
            .Where(f => f.OutstandingBalance > 0)
            .OrderByDescending(f => f.OutstandingBalance)
            .Select(f => new FinanciamientoDto(
                f.Concept,
                f.Installments,
                f.InstallmentAmount,
                f.OutstandingBalance,
                f.CurrencyCode))
            .ToListAsync(ct);

        var latestLoan = await db.LoanStatements
            .AsNoTracking()
            .Include(s => s.Payments)
            .OrderByDescending(s => s.CreatedUtc)
            .FirstOrDefaultAsync(ct);

        List<PrestamoDto> prestamos = [];
        if (latestLoan is not null)
        {
            var lastPayment = latestLoan.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            prestamos.Add(new PrestamoDto(
                "Coopealianza",
                lastPayment?.Total ?? 0m,
                lastPayment?.Capital ?? 0m,
                lastPayment?.Interest ?? 0m,
                latestLoan.OutstandingBalance,
                lastPayment?.PaymentDate));
        }

        var totalFinancingsCrc = financings
            .Where(f => f.CurrencyCode == "CRC")
            .Sum(f => f.InstallmentAmount);

        var totalLoansCrc = prestamos.Sum(p => p.CuotaTotal);

        return TypedResults.Ok(new LoansResponse(financings, prestamos, totalFinancingsCrc + totalLoansCrc));
    }
}

public record FinanciamientoDto(
    string Concept,
    string Installments,
    decimal InstallmentAmount,
    decimal OutstandingBalance,
    string CurrencyCode);

public record PrestamoDto(
    string Nombre,
    decimal CuotaTotal,
    decimal Capital,
    decimal Interest,
    decimal SaldoVigente,
    DateOnly? UltimoPago);

public record LoansResponse(
    List<FinanciamientoDto> Financiamientos,
    List<PrestamoDto> Prestamos,
    decimal TotalMensualCrc);
