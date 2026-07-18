using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Parsing;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bancos.Api.Features.Imports;
public sealed class ImportJobs(BancosDbContext db, ImportTemplateDetector detector, BcrDebitCsvParser bcrParser, ILogger<ImportJobs> logger)
{
    public async Task ProcessAsync(Guid importId, PerformContext? context)
    {
        var import = await db.Imports.SingleOrDefaultAsync(x => x.Id == importId) ?? throw new InvalidOperationException("Import was not found.");
        if (import.Status == ImportStatus.Completed) return;
        import.Status = ImportStatus.Processing; await db.SaveChangesAsync(); WriteStage(context, "Starting import."); logger.LogInformation("Processing import {ImportId}", importId);
        try
        {
            WriteStage(context, "Detecting template from content.");
            var detection = detector.Detect(await File.ReadAllBytesAsync(import.TemporaryPath));
            if (!detection.IsKnown) throw new InvalidDataException("No unique documented template matched this file.");
            import.Template = detection.Template;
            WriteStage(context, "Template detected: {0}", detection.Template);
            if (detection.Template != ImportTemplates.BcrDebitCsvV1) throw new InvalidDataException($"Template '{detection.Template}' is recognized but its extractor is not enabled yet.");
            WriteStage(context, "Extracting and validating BCR debit movements.");
            var movements = bcrParser.Parse(await File.ReadAllTextAsync(import.TemporaryPath));
            var fingerprints = movements.Select(movement => CreateFingerprint(import.AccountAuxiliaryId, movement)).ToArray();
            var existing = await db.Transactions.Where(transaction => transaction.AccountAuxiliaryId == import.AccountAuxiliaryId && fingerprints.Contains(transaction.SourceFingerprint)).Select(transaction => transaction.SourceFingerprint).ToListAsync();
            foreach (var movement in movements.Zip(fingerprints))
            {
                if (existing.Contains(movement.Second)) continue;
                db.Transactions.Add(new Transaction
                {
                    ImportId = import.Id,
                    AccountAuxiliaryId = import.AccountAuxiliaryId,
                    BookingDate = movement.First.BookingDate,
                    ExternalReference = movement.First.ExternalReference,
                    SourceFingerprint = movement.Second,
                    AmountCrc = movement.First.Credit - movement.First.Debit,
                    OriginalAmount = movement.First.Credit - movement.First.Debit,
                    DescriptionNormalized = ImportTemplateDetector.Normalize(movement.First.Description)
                });
            }
            WriteStage(context, "Validated {0} movements and persisted the new fingerprints.", movements.Count);
            import.Status = ImportStatus.Completed; import.ProcessedUtc = DateTime.UtcNow; await db.SaveChangesAsync(); WriteStage(context, "Import completed.");
        }
        catch (Exception exception) { import.Status = ImportStatus.Failed; import.FailureReason = exception.Message; await db.SaveChangesAsync(); throw; }
        finally { if (File.Exists(import.TemporaryPath)) File.Delete(import.TemporaryPath); }
    }

    private static void WriteStage(PerformContext? context, string message, params object[] arguments) => context?.WriteLine(message, arguments);
    private static string CreateFingerprint(Guid accountAuxiliaryId, ParsedBankMovement movement)
    {
        var source = string.Join('|', accountAuxiliaryId, movement.BookingDate, movement.ExternalReference.Trim(), ImportTemplateDetector.Normalize(movement.Description), movement.Debit, movement.Credit, "CRC");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
