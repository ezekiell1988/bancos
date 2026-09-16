using System.ComponentModel;
using Bancos.Mcp.Catalog;
using Bancos.Mcp.Features.TemplateDetection;
using Hangfire;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.FileProcessing;

[McpServerToolType]
public static class ProcessImportFileTool
{
    [McpServerTool(Name = "process_import_file", Title = "Procesar archivos de importación bancaria",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Recibe una lista de rutas relativas de archivos bancarios (CSV, XLS, PDF, HTML). "
               + "Detecta automáticamente la plantilla, resuelve la cuenta bancaria y encola un job de Hangfire por archivo para parsear y persistir los datos. "
               + "Devuelve el archivo y el jobId de cada job encolado; use get_import_job_status con ese jobId para saber si terminó o falló.")]
    public static async Task<ProcessImportFileResult> ProcessImportFileAsync(
        ImportTemplateDetectionService detectionService,
        AccountResolver accountResolver,
        IBackgroundJobClient jobClient,
        [Description("Rutas relativas de archivos a procesar, ejemplo: [\"Coopealianza.pdf\", \"BAC_corte.csv\"]")] IReadOnlyList<string> files,
        CancellationToken cancellationToken = default)
    {
        var relativePaths = files.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (relativePaths.Count == 0)
            throw new McpException("La lista de archivos está vacía.");

        var jobs = new List<ProcessedFileJob>();
        foreach (var relativePath in relativePaths)
        {
            try
            {
                var templateId = await detectionService.DetectAsync(relativePath, cancellationToken);
                var definition = ImportTemplateCatalog.Definitions.FirstOrDefault(d => d.Id == templateId)
                    ?? throw new InvalidOperationException("Plantilla no encontrada en catálogo.");

                var fullPath = detectionService.ResolveFullPath(relativePath);
                var fileContent = await File.ReadAllBytesAsync(fullPath, cancellationToken);

                Guid primaryAccountId;
                Guid? secondaryAccountId = null;
                if (definition.ParserKey == "bac-credit-financing-xls")
                {
                    var pair = await accountResolver.ResolveFinancingPairByPathAsync(relativePath, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey == "bac-credit-online-pdf")
                {
                    primaryAccountId = await accountResolver.ResolveCrcByPathAsync(relativePath, cancellationToken);
                }
                else if (definition.ParserKey == "bac-credit-csv")
                {
                    var pair = await accountResolver.ResolveFinancingPairByPathAsync(relativePath, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey == "bn-card-statement-pdf")
                {
                    var pair = await accountResolver.ResolveBnCardStatementPairAsync(
                        templateId, fileContent, cancellationToken);
                    primaryAccountId = pair.CrcAccountId;
                    secondaryAccountId = pair.UsdAccountId;
                }
                else if (definition.ParserKey is "bcr-debit-csv" or "bn-debit-csv" or "bn-debit-csv-crc")
                {
                    var resolved = await accountResolver.TryResolveDebitCsvByIbanPathAsync(
                        relativePath, cancellationToken);
                    if (resolved.HasValue)
                    {
                        primaryAccountId = resolved.Value.AccountId;
                        templateId = resolved.Value.TemplateId;
                        definition = ImportTemplateCatalog.Definitions.First(d => d.Id == templateId);
                    }
                    else
                    {
                        primaryAccountId = await accountResolver.ResolveAsync(
                            templateId, null, fileContent, cancellationToken);
                    }
                }
                else if (definition.ParserKey == "bank-account-movements-xls")
                {
                    primaryAccountId = await accountResolver.ResolveLinkedAccountByIbanPathAsync(
                        relativePath, templateId, cancellationToken);
                }
                else
                {
                    primaryAccountId = await accountResolver.ResolveAsync(templateId, null, fileContent, cancellationToken);
                }

                var jobId = jobClient.Enqueue<ImportFileJob>(job =>
                    job.ExecuteAsync(fullPath, definition.ParserKey, primaryAccountId, secondaryAccountId, null!));

                jobs.Add(new ProcessedFileJob(relativePath, jobId, definition.Code, "enqueued", null));
            }
            catch (Exception ex)
            {
                jobs.Add(new ProcessedFileJob(relativePath, null, null, "error", ex.Message));
            }
        }

        return new ProcessImportFileResult(jobs);
    }
}

public sealed record ProcessedFileJob(string File, string? JobId, string? Template, string Status, string? Error);

public sealed record ProcessImportFileResult(IReadOnlyList<ProcessedFileJob> Jobs);
