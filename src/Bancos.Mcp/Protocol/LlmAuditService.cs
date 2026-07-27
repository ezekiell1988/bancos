using System.Text.Json;

namespace Bancos.Mcp.Protocol;

public interface ILlmAuditService
{
    void LogEvent(string category, string intent, string result, object? context = null);
}

public sealed class LlmAuditService(IHostEnvironment environment) : ILlmAuditService
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;
    private static readonly Lock FileLock = new();
    private readonly string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "llm-audit.md");

    public void LogEvent(string category, string intent, string result, object? context = null)
    {
        if (!environment.IsDevelopment())
            return;

        var safeContext = context is null ? string.Empty : $" Context: {JsonSerializer.Serialize(context)}";
        var entry = $"## {DateTimeOffset.Now:O} [{category}]\nIntent: {intent}\nResult: {result}{safeContext}\n\n";

        lock (FileLock)
        {
            var directory = Path.GetDirectoryName(logPath)!;
            Directory.CreateDirectory(directory);
            if (File.Exists(logPath) && new FileInfo(logPath).Length + entry.Length > MaxFileSizeBytes)
                File.WriteAllText(logPath, string.Empty);

            File.AppendAllText(logPath, entry);
        }
    }
}
