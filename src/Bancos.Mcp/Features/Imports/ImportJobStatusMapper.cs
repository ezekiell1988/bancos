namespace Bancos.Mcp.Features.Imports;

public sealed record ImportJobStateEntry(string StateName, DateTimeOffset CreatedAt, IReadOnlyDictionary<string, string> Data);

public sealed record ImportJobSnapshot(string? FileName, string? ParserKey, IReadOnlyList<ImportJobStateEntry> History);

public sealed record ImportJobStatusResult(
    string JobId,
    string Status,
    string? FileName,
    string? ParserKey,
    DateTimeOffset? EnqueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ResultSummary,
    string? ErrorMessage,
    string? ErrorDetails,
    bool CanRetry,
    string NextStep);

public static class ImportJobStatusMapper
{
    private const int MaxErrorDetailsLength = 4000;

    public static ImportJobStatusResult Map(string jobId, ImportJobSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.History.Count == 0)
            return new ImportJobStatusResult(
                jobId, "desconocido", null, null, null, null, null, null, null, null, false,
                "El job no existe en Hangfire o ya expiró (retención por defecto: 1 día). Verifique el ID o consulte los movimientos importados directamente.");

        var current = snapshot.History.OrderByDescending(h => h.CreatedAt).First();
        var enqueued = snapshot.History.FirstOrDefault(h => h.StateName == "Enqueued");
        var processing = snapshot.History.FirstOrDefault(h => h.StateName == "Processing");
        var finished = snapshot.History.FirstOrDefault(h => h.StateName is "Succeeded" or "Failed");

        var status = current.StateName switch
        {
            "Enqueued" => "en_cola",
            "Scheduled" => "programado",
            "Processing" => "procesando",
            "Succeeded" => "completado",
            "Failed" => "error",
            "Deleted" => "eliminado",
            _ => "desconocido"
        };

        string? resultSummary = null;
        string? errorMessage = null;
        string? errorDetails = null;
        if (current.StateName == "Succeeded" && current.Data.TryGetValue("Result", out var rawResult))
            resultSummary = TrimJsonQuotes(rawResult);

        if (current.StateName == "Failed")
        {
            current.Data.TryGetValue("ExceptionMessage", out errorMessage);
            if (current.Data.TryGetValue("ExceptionDetails", out var details))
                errorDetails = details.Length > MaxErrorDetailsLength
                    ? details[..MaxErrorDetailsLength] + "…(truncado)"
                    : details;
        }

        var canRetry = status == "error";
        var nextStep = status switch
        {
            "error" => "Revise el error y, si ya se corrigió la causa, llame retry_import_job con este jobId.",
            "en_cola" or "programado" or "procesando" => "Aún no finaliza; consulte de nuevo en unos segundos.",
            "completado" => "Sin acción requerida.",
            _ => "Sin acción disponible."
        };

        return new ImportJobStatusResult(
            jobId, status, snapshot.FileName, snapshot.ParserKey,
            enqueued?.CreatedAt, processing?.CreatedAt, finished?.CreatedAt,
            resultSummary, errorMessage, errorDetails, canRetry, nextStep);
    }

    private static string TrimJsonQuotes(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
}
