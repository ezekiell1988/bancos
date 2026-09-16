using System.ComponentModel;
using Bancos.Mcp.Features.FileProcessing;
using Hangfire;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Imports;

[McpServerToolType]
public static class RetryImportJobTool
{
    [McpServerTool(Name = "retry_import_job", Title = "Reintentar un job de importación fallido",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Reencola en Hangfire un job de importación que terminó en error, usando los mismos identificadores "
               + "(ruta de archivo, plantilla y cuentas) que el job original; no reenvía bytes del archivo. "
               + "Solo se permite cuando el job consultado está en estado 'error'.")]
    public static RetryImportJobResult RetryImportJob(
        ImportJobQueryService queryService,
        IBackgroundJobClient jobClient,
        [Description("ID del job de Hangfire que falló.")] string jobId)
    {
        var current = queryService.GetStatus(jobId);
        if (current.Status != "error")
            throw new McpException(
                $"Solo se puede reintentar un job en estado 'error'. Estado actual de {jobId}: '{current.Status}'.");

        var retryArgs = queryService.GetRetryArgs(jobId);
        if (retryArgs is null)
            throw new McpException(
                $"No fue posible recuperar los parámetros originales del job {jobId} (puede haber expirado en Hangfire).");

        if (!File.Exists(retryArgs.FilePath))
            throw new McpException(
                $"El archivo original ya no existe en el servidor: {Path.GetFileName(retryArgs.FilePath)}.");

        // ExecuteAsync solo llama SaveChangesAsync al final, así que un intento fallido no deja escritura parcial: el reintento es seguro.
        var newJobId = jobClient.Enqueue<ImportFileJob>(job =>
            job.ExecuteAsync(retryArgs.FilePath, retryArgs.ParserKey, retryArgs.BankAccountId, retryArgs.UsdBankAccountId, null!));

        return new RetryImportJobResult(jobId, newJobId, "en_cola");
    }
}

public sealed record RetryImportJobResult(string OriginalJobId, string NewJobId, string Status);
