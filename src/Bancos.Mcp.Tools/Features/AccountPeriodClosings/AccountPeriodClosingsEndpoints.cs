using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Bancos.Mcp.Features.AccountPeriodClosings;

public static class AccountPeriodClosingsEndpoints
{
    public static IEndpointRouteBuilder MapAccountPeriodClosingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account-period-closings/calculate", (
            [FromBody] CalculateRequest request,
            IBackgroundJobClient jobClient) =>
        {
            var jobId = jobClient.Enqueue<CalculateAccountPeriodClosingsJob>(
                job => job.ExecuteAsync(request.PeriodId, null!));
            return Results.Ok(new { jobId });
        });

        return endpoints;
    }

    public sealed record CalculateRequest(Guid PeriodId);
}
