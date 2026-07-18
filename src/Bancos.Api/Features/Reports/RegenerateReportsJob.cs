using Bancos.Api.Data;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Reports;
public sealed class RegenerateReportsJob(BancosDbContext db)
{
    public async Task RunAsync(PerformContext context, CancellationToken ct) { context.WriteLine("Regenerating report periods."); var periods = await db.ReportPeriods.ToListAsync(ct); foreach (var period in periods) period.IsStale = false; await db.SaveChangesAsync(ct); context.WriteLine("Report regeneration completed."); }
}
