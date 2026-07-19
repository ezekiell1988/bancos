using Bancos.Api.Domain;
using Bancos.Api.Features.Imports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class ImportProgressReporterTests
{
    [Fact]
    public async Task Reporter_throttles_subpercent_updates_and_keeps_progress_monotonic_within_an_attempt()
    {
        var store = new MemoryProgressStore();
        var publisher = new RecordingProgressPublisher();
        var clock = new TestTimeProvider();
        var reporter = CreateReporter(store, publisher, clock);
        var importId = Guid.NewGuid();

        var attempt = await reporter.BeginAttemptAsync(importId, null);
        await reporter.ReportAsync(importId, attempt, ImportProgressStages.Classifying, 10, 100, 10);
        await reporter.ReportAsync(importId, attempt, ImportProgressStages.Classifying, 11, 100, 10);
        await reporter.ReportAsync(importId, attempt, ImportProgressStages.Classifying, 9, 100, 9);
        await reporter.ReportAsync(importId, attempt, ImportProgressStages.Classifying, 12, 100, 11);

        var progress = Assert.Single(store.Values.Values);
        Assert.Equal(11, progress.Percent);
        Assert.Equal(3, publisher.Values.Count); // inicio, 10% y 11%; los dos cambios limitados no se emiten
    }

    [Fact]
    public async Task Reporter_resets_progress_for_a_new_attempt_and_always_emits_terminal_status()
    {
        var store = new MemoryProgressStore();
        var publisher = new RecordingProgressPublisher();
        var reporter = CreateReporter(store, publisher, new TestTimeProvider());
        var importId = Guid.NewGuid();

        var firstAttempt = await reporter.BeginAttemptAsync(importId, null);
        await reporter.ReportAsync(importId, firstAttempt, ImportProgressStages.Classifying, 80, 100, 80);
        await reporter.ReportAsync(importId, firstAttempt, ImportProgressStages.Failed, 80, 100, 80, ImportStatus.Failed);
        var secondAttempt = await reporter.BeginAttemptAsync(importId, null);

        var progress = Assert.Single(store.Values.Values);
        Assert.Equal(2, secondAttempt);
        Assert.Equal(2, progress.Attempt);
        Assert.Equal(0, progress.Percent);
        Assert.Equal(ImportProgressStages.Starting, progress.Stage);
        Assert.Contains(publisher.Values, item => item.Status == nameof(ImportStatus.Failed));
    }

    private static ImportProgressReporter CreateReporter(MemoryProgressStore store, RecordingProgressPublisher publisher, TimeProvider clock) =>
        new(store, publisher, Options.Create(new ImportProgressOptions { MinimumIntervalSeconds = 1 }), clock, NullLogger<ImportProgressReporter>.Instance);

    private sealed class MemoryProgressStore : IImportProgressStore
    {
        public Dictionary<Guid, ImportProgressResponse> Values { get; } = [];
        public Task<ImportProgressResponse?> GetAsync(Guid importId, CancellationToken cancellationToken) => Task.FromResult(Values.GetValueOrDefault(importId));
        public Task SaveAsync(ImportProgressResponse progress, CancellationToken cancellationToken) { Values[progress.ImportId] = progress; return Task.CompletedTask; }
    }

    private sealed class RecordingProgressPublisher : IImportProgressPublisher
    {
        public List<ImportProgressResponse> Values { get; } = [];
        public Task PublishAsync(ImportProgressResponse progress, CancellationToken cancellationToken) { Values.Add(progress); return Task.CompletedTask; }
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 7, 19, 0, 0, 0, TimeSpan.Zero);
    }
}
