using Bancos.Mcp.Features.Imports;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ImportJobStatusMapperTests
{
    private static ImportJobStateEntry State(string name, DateTimeOffset at, Dictionary<string, string>? data = null) =>
        new(name, at, data ?? new Dictionary<string, string>());

    [Fact]
    public void Map_returns_unknown_when_job_not_found()
    {
        var result = ImportJobStatusMapper.Map("job-1", null);

        Assert.Equal("desconocido", result.Status);
        Assert.False(result.CanRetry);
        Assert.Contains("expiró", result.NextStep);
    }

    [Fact]
    public void Map_reports_succeeded_job_with_result_summary()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new ImportJobSnapshot("BAC.csv", "bcr-debit-csv", new[]
        {
            State("Enqueued", now.AddMinutes(-2)),
            State("Processing", now.AddMinutes(-1)),
            State("Succeeded", now, new Dictionary<string, string> { ["Result"] = "\"Procesamiento completado.\"" })
        });

        var result = ImportJobStatusMapper.Map("job-2", snapshot);

        Assert.Equal("completado", result.Status);
        Assert.Equal("Procesamiento completado.", result.ResultSummary);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.CanRetry);
    }

    [Fact]
    public void Map_reports_failed_job_with_error_and_allows_retry()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new ImportJobSnapshot("Coopealianza.pdf", "coopealianza-loan-pdf", new[]
        {
            State("Enqueued", now.AddMinutes(-2)),
            State("Failed", now, new Dictionary<string, string>
            {
                ["ExceptionMessage"] = "No existe ningún tipo de cambio USD disponible.",
                ["ExceptionDetails"] = "System.IO.InvalidDataException: ..."
            })
        });

        var result = ImportJobStatusMapper.Map("job-3", snapshot);

        Assert.Equal("error", result.Status);
        Assert.Equal("No existe ningún tipo de cambio USD disponible.", result.ErrorMessage);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.CanRetry);
    }

    [Fact]
    public void Map_reports_processing_job_as_not_retryable()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new ImportJobSnapshot("BAC.csv", "bcr-debit-csv", new[]
        {
            State("Enqueued", now.AddMinutes(-1)),
            State("Processing", now)
        });

        var result = ImportJobStatusMapper.Map("job-4", snapshot);

        Assert.Equal("procesando", result.Status);
        Assert.False(result.CanRetry);
    }

    [Fact]
    public void Map_truncates_very_long_error_details()
    {
        var now = DateTimeOffset.UtcNow;
        var longDetails = new string('x', 5000);
        var snapshot = new ImportJobSnapshot("BAC.csv", "bcr-debit-csv", new[]
        {
            State("Failed", now, new Dictionary<string, string>
            {
                ["ExceptionMessage"] = "error",
                ["ExceptionDetails"] = longDetails
            })
        });

        var result = ImportJobStatusMapper.Map("job-5", snapshot);

        Assert.True(result.ErrorDetails!.Length < 5000);
        Assert.EndsWith("(truncado)", result.ErrorDetails);
    }
}
