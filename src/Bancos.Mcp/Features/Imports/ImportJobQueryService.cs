using Bancos.Mcp.Features.FileProcessing;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Bancos.Mcp.Features.Imports;

public sealed class ImportJobQueryService
{
    // JobStorage.Current queda establecido por UseSqlServerStorage() al arrancar; no hay registro propio en DI para este tipo.
    private static IMonitoringApi MonitoringApi => JobStorage.Current.GetMonitoringApi();

    public ImportJobStatusResult GetStatus(string jobId)
    {
        var details = MonitoringApi.JobDetails(jobId);
        return ImportJobStatusMapper.Map(jobId, ToSnapshot(details));
    }

    public IReadOnlyList<RecentImportJob> ListRecent(int itemsPerPage, string? statusFilter)
    {
        var monitoringApi = MonitoringApi;
        var jobs = new List<RecentImportJob>();

        foreach (var (jobId, job) in monitoringApi.EnqueuedJobs("default", 0, 200))
            if (IsImportJob(job.Job))
                jobs.Add(new RecentImportJob(jobId, FileNameOf(job.Job), "en_cola", job.EnqueuedAt is { } e ? new DateTimeOffset(e, TimeSpan.Zero) : null, null));

        foreach (var (jobId, job) in monitoringApi.ProcessingJobs(0, 200))
            if (IsImportJob(job.Job))
                jobs.Add(new RecentImportJob(jobId, FileNameOf(job.Job), "procesando", job.StartedAt is { } s ? new DateTimeOffset(s, TimeSpan.Zero) : null, null));

        foreach (var (jobId, job) in monitoringApi.SucceededJobs(0, 200))
            if (IsImportJob(job.Job))
                jobs.Add(new RecentImportJob(jobId, FileNameOf(job.Job), "completado", job.SucceededAt is { } su ? new DateTimeOffset(su, TimeSpan.Zero) : null, job.Result?.ToString()));

        foreach (var (jobId, job) in monitoringApi.FailedJobs(0, 200))
            if (IsImportJob(job.Job))
                jobs.Add(new RecentImportJob(jobId, FileNameOf(job.Job), "error", job.FailedAt is { } f ? new DateTimeOffset(f, TimeSpan.Zero) : null, job.ExceptionMessage));

        var filtered = statusFilter is null
            ? jobs
            : jobs.Where(j => j.Status == statusFilter).ToList();

        return filtered
            .OrderByDescending(j => j.At)
            .Take(itemsPerPage)
            .ToList();
    }

    public ImportRetryArgs? GetRetryArgs(string jobId)
    {
        var details = MonitoringApi.JobDetails(jobId);
        if (details?.Job is not { } job || !IsImportJob(job) || job.Args is not { Count: >= 3 } args) return null;
        if (args[0] is not string filePath || args[1] is not string parserKey || args[2] is not Guid bankAccountId) return null;
        var usdBankAccountId = args.Count > 3 && args[3] is Guid usd ? usd : (Guid?)null;
        return new ImportRetryArgs(filePath, parserKey, bankAccountId, usdBankAccountId);
    }

    private static bool IsImportJob(Hangfire.Common.Job? job) =>
        job?.Type == typeof(ImportFileJob);

    private static string? FileNameOf(Hangfire.Common.Job? job) =>
        job?.Args is { Count: > 0 } args && args[0] is string filePath ? Path.GetFileName(filePath) : null;

    private static ImportJobSnapshot? ToSnapshot(JobDetailsDto? details)
    {
        if (details is null) return null;

        string? fileName = null;
        string? parserKey = null;
        if (details.Job?.Args is { Count: >= 2 } args)
        {
            fileName = args[0] as string is { } path ? Path.GetFileName(path) : null;
            parserKey = args[1] as string;
        }

        var history = details.History
            .Select(h => new ImportJobStateEntry(h.StateName, new DateTimeOffset(h.CreatedAt, TimeSpan.Zero), new Dictionary<string, string>(h.Data ?? new Dictionary<string, string>())))
            .ToList();

        return new ImportJobSnapshot(fileName, parserKey, history);
    }
}

public sealed record RecentImportJob(string JobId, string? FileName, string Status, DateTimeOffset? At, string? Detail);

public sealed record ImportRetryArgs(string FilePath, string ParserKey, Guid BankAccountId, Guid? UsdBankAccountId);
