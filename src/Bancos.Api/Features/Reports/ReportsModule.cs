using Bancos.Api.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Reports;
public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services) => services.AddScoped<RegenerateReportsJob>();
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/regenerate", async (IBackgroundJobClient jobs, BancosDbContext db, CancellationToken ct) => { var periods = await db.ReportPeriods.ToListAsync(ct); foreach (var period in periods) period.IsStale = true; await db.SaveChangesAsync(ct); var id = jobs.Enqueue<RegenerateReportsJob>(x => x.RunAsync(null!, CancellationToken.None)); return TypedResults.Accepted($"/hangfire/jobs/details/{id}", new { jobId = id }); }).WithTags("Reports");
        return app;
    }
}
