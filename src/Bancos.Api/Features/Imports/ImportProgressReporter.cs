using System.ComponentModel.DataAnnotations;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bancos.Api.Features.Imports;

public static class ImportProgressStages
{
    public const string Queued = "Queued";
    public const string Starting = "Starting";
    public const string DetectingTemplate = "DetectingTemplate";
    public const string Extracting = "Extracting";
    public const string Classifying = "Classifying";
    public const string Persisting = "Persisting";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public sealed class ImportProgressOptions
{
    public const string Section = "ImportProgress";
    [Range(1, int.MaxValue)] public int MinimumIntervalSeconds { get; set; } = 1;
}

public sealed record ImportProgressResponse(Guid ImportId, int Attempt, string Stage, int Current, int Total, int Percent, string Status, DateTime UpdatedUtc);

public interface IImportProgressStore
{
    Task<ImportProgressResponse?> GetAsync(Guid importId, CancellationToken cancellationToken);
    Task SaveAsync(ImportProgressResponse progress, CancellationToken cancellationToken);
}

public sealed class EfImportProgressStore(IServiceScopeFactory scopeFactory) : IImportProgressStore
{
    public async Task<ImportProgressResponse?> GetAsync(Guid importId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancosDbContext>();
        return await db.ImportProgress.AsNoTracking()
            .Where(x => x.ImportId == importId)
            .Select(x => new ImportProgressResponse(x.ImportId, x.Attempt, x.Stage, x.Current, x.Total, x.Percent, x.Status.ToString(), x.UpdatedUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(ImportProgressResponse progress, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancosDbContext>();
        var entity = await db.ImportProgress.SingleOrDefaultAsync(x => x.ImportId == progress.ImportId, cancellationToken);
        var status = Enum.Parse<ImportStatus>(progress.Status);
        if (entity is null)
        {
            db.ImportProgress.Add(new ImportProgress
            {
                ImportId = progress.ImportId,
                Attempt = progress.Attempt,
                Stage = progress.Stage,
                Current = progress.Current,
                Total = progress.Total,
                Percent = progress.Percent,
                Status = status,
                UpdatedUtc = progress.UpdatedUtc
            });
        }
        else
        {
            if (progress.Attempt < entity.Attempt || progress.Attempt == entity.Attempt && progress.Percent < entity.Percent) return;
            entity.Attempt = progress.Attempt;
            entity.Stage = progress.Stage;
            entity.Current = progress.Current;
            entity.Total = progress.Total;
            entity.Percent = progress.Percent;
            entity.Status = status;
            entity.UpdatedUtc = progress.UpdatedUtc;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface IImportProgressReporter
{
    Task<int> BeginAttemptAsync(Guid importId, PerformContext? context, CancellationToken cancellationToken = default);
    Task ReportAsync(Guid importId, int attempt, string stage, int current, int total, int percent, ImportStatus status = ImportStatus.Processing, CancellationToken cancellationToken = default);
}

public sealed class ImportProgressReporter(
    IImportProgressStore store,
    IImportProgressPublisher publisher,
    IOptions<ImportProgressOptions> options,
    TimeProvider timeProvider,
    ILogger<ImportProgressReporter> logger) : IImportProgressReporter
{
    private readonly Dictionary<Guid, ReporterState> states = [];
    private readonly TimeSpan minimumInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.MinimumIntervalSeconds));

    public async Task<int> BeginAttemptAsync(Guid importId, PerformContext? context, CancellationToken cancellationToken = default)
    {
        var previous = await store.GetAsync(importId, cancellationToken);
        var attempt = (previous?.Attempt ?? 0) + 1;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var snapshot = new ImportProgressResponse(importId, attempt, ImportProgressStages.Starting, 0, 0, 0, ImportStatus.Processing.ToString(), now);
        var progressBar = context?.WriteProgressBar("Import progress", 0, ConsoleTextColor.Green);
        states[importId] = new ReporterState(snapshot, now, progressBar, context);
        context?.WriteLine("Stage: {0}", ImportProgressStages.Starting);
        await SaveAndPublish(snapshot, cancellationToken);
        return attempt;
    }

    public async Task ReportAsync(Guid importId, int attempt, string stage, int current, int total, int percent, ImportStatus status = ImportStatus.Processing, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        current = Math.Max(0, current);
        total = Math.Max(current, total);
        percent = Math.Clamp(percent, 0, 100);
        if (!states.TryGetValue(importId, out var state))
        {
            var persisted = await store.GetAsync(importId, cancellationToken);
            var initial = persisted is not null && persisted.Attempt == attempt
                ? persisted
                : new ImportProgressResponse(importId, attempt, stage, current, total, 0, status.ToString(), now);
            state = new ReporterState(initial, initial.UpdatedUtc, null, null);
            states[importId] = state;
        }
        if (attempt != state.Last.Attempt) return;

        if (total == 0 && state.Last.Total > 0) { current = state.Last.Current; total = state.Last.Total; }
        percent = Math.Max(percent, state.Last.Percent);
        var terminal = status is ImportStatus.Completed or ImportStatus.Failed;
        var stageChanged = !string.Equals(stage, state.Last.Stage, StringComparison.Ordinal);
        var shouldEmit = terminal || stageChanged || percent >= state.Last.Percent + 1 || now - state.LastEmittedUtc >= minimumInterval;
        if (!shouldEmit) return;

        var snapshot = new ImportProgressResponse(importId, attempt, stage, current, total, percent, status.ToString(), now);
        if (stageChanged) state.ProgressContext?.WriteLine("Stage: {0}", stage);
        state.ProgressBar?.SetValue(percent);
        state.Last = snapshot;
        state.LastEmittedUtc = now;
        await SaveAndPublish(snapshot, cancellationToken);
    }

    private async Task SaveAndPublish(ImportProgressResponse snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveAsync(snapshot, cancellationToken);
            await publisher.PublishAsync(snapshot, cancellationToken);
        }
        catch (Exception)
        {
            logger.LogWarning("Import progress could not be persisted or published for {ImportId}.", snapshot.ImportId);
        }
    }

    private sealed class ReporterState(ImportProgressResponse last, DateTime lastEmittedUtc, IProgressBar? progressBar, PerformContext? progressContext)
    {
        public ImportProgressResponse Last { get; set; } = last;
        public DateTime LastEmittedUtc { get; set; } = lastEmittedUtc;
        public IProgressBar? ProgressBar { get; } = progressBar;
        public PerformContext? ProgressContext { get; } = progressContext;
    }
}

public sealed class NullImportProgressReporter : IImportProgressReporter
{
    public static NullImportProgressReporter Instance { get; } = new();
    public Task<int> BeginAttemptAsync(Guid importId, PerformContext? context, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task ReportAsync(Guid importId, int attempt, string stage, int current, int total, int percent, ImportStatus status = ImportStatus.Processing, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
