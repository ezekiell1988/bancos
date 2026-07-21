using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Infrastructure;
using Bancos.Api.Features.Parsing;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bancos.Api.Features.Imports;

public static class ImportsModule
{
    public static IServiceCollection AddImportsModule(this IServiceCollection services)
    {
        services.AddScoped<ImportJobs>(); services.AddScoped<BacCreditFinancingXlsParser>(); services.AddScoped<AccountMovementSpreadsheetParser>(); services.AddScoped<CardStatementParser>(); services.AddScoped<CoopealianzaLoanPdfParser>(); services.AddScoped<BacAccountStatementPdfParser>(); services.AddScoped<BnCardStatementPdfParser>();
        services.AddScoped<IImportJobScheduler, HangfireImportJobScheduler>(); services.AddScoped<ImportTemplatePatternService>();
        services.AddOptions<ImportProgressOptions>().BindConfiguration(ImportProgressOptions.Section).ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IImportProgressStore, EfImportProgressStore>();
        services.AddSingleton<IImportProgressPublisher, SignalRImportProgressPublisher>();
        services.AddScoped<IImportProgressReporter, ImportProgressReporter>();
        services.AddSingleton<ImportTemplateDetector>();
        services.AddSingleton<BcrDebitCsvParser>();
        return services;
    }
    public static IEndpointRouteBuilder MapImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports");
        group.MapPost("/preview", Preview).DisableAntiforgery();
        group.MapPost("/learn", Learn).DisableAntiforgery();
        group.MapPost("/upload", Upload).DisableAntiforgery();
        group.MapGet("/", List);
        group.MapGet("/{id:guid}/progress", GetProgress);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/{id:guid}/retry", Retry);
        return app;
    }
    private static async Task<Ok<ImportPreviewBatchResponse>> Preview(IFormFile file, BancosDbContext db, ImportTemplatePatternService patterns, CancellationToken ct)
    {
        var sources = ZipImportReader.Read(file.FileName, await ReadContent(file, ct));
        var entries = new List<ImportPreviewEntryResponse>();
        foreach (var source in sources)
        {
            try { var detection = await patterns.DetectAsync(source.Content, ct); var plan = await ResolvePlan(detection.Template, db, ct); entries.Add(new(source.EntryIndex, source.Path, ToPreviewResponse(detection, plan))); }
            catch (Exception) when (source.Content.Length > 0) { entries.Add(new(source.EntryIndex, source.Path, new(ImportTemplates.Unknown, "No se pudo analizar", "unsupported", "Este archivo interno no se puede procesar."))); }
        }
        return TypedResults.Ok(new ImportPreviewBatchResponse(entries));
    }

    private static async Task<Results<NoContent, ValidationProblem>> Learn(IFormFile file, [FromForm] string entryPath, [FromForm] int? entryIndex, [FromForm] string template, ImportTemplatePatternService patterns, CancellationToken ct)
    {
        if (ImportReviewTemplates.Get(template) is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["template"] = ["El tipo confirmado no es válido."] });
        var source = FindSource(ZipImportReader.Read(file.FileName, await ReadContent(file, ct)), entryPath, entryIndex);
        if (source is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["entryPath"] = ["No se encontró el archivo dentro del ZIP."] });
        await patterns.LearnAsync(source.Content, template, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<ImportResponse>, ValidationProblem>> Upload(IFormFile file, [FromForm] string? entryPath, [FromForm] int? entryIndex, [FromForm] string? template, [FromForm] bool force, BancosDbContext db, ImportTemplatePatternService patterns, IImportJobScheduler scheduler, IOptions<StorageOptions> storage, CancellationToken ct)
    {
        if (file.Length == 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["El archivo no puede estar vacío."] });
        var sources = ZipImportReader.Read(file.FileName, await ReadContent(file, ct));
        var source = FindSource(sources, entryPath, entryIndex);
        if (source is null) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["No se encontró el archivo seleccionado dentro del ZIP."] });
        var detection = await patterns.DetectAsync(source.Content, ct);
        var selectedTemplate = string.IsNullOrWhiteSpace(template) ? detection.Template : template;
        var plan = await ResolvePlan(selectedTemplate, db, ct);
        if (plan.Error is not null) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["template"] = [plan.Error] });

        var hash = Convert.ToHexString(SHA256.HashData(source.Content));
        if (!force && await db.ImportFingerprints.AnyAsync(x => x.Hash == hash, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["An identical import already exists."] });

        Directory.CreateDirectory(storage.Value.TemporaryPath);
        var path = Path.Combine(storage.Value.TemporaryPath, $"{Guid.NewGuid():N}.upload");
        await File.WriteAllBytesAsync(path, source.Content, ct);
        if (!string.IsNullOrWhiteSpace(template) && detection.Template == ImportTemplates.Unknown) await patterns.LearnAsync(source.Content, selectedTemplate, ct);
        var import = new Import { FileName = Path.GetFileName(source.Path), TemporaryPath = path, ContentHash = hash, AccountAuxiliaryId = plan.AccountAuxiliaryId!.Value, Template = selectedTemplate };
        db.Imports.Add(import);
        db.ImportProgress.Add(new ImportProgress { ImportId = import.Id, Attempt = 0, Stage = ImportProgressStages.Queued, Current = 0, Total = 0, Percent = 0, Status = ImportStatus.Queued, UpdatedUtc = DateTime.UtcNow });
        if (!force) db.ImportFingerprints.Add(new ImportFingerprint { Hash = hash, ImportId = import.Id });
        await db.SaveChangesAsync(ct);
        scheduler.Enqueue(import.Id);
        return TypedResults.Created($"/api/imports/{import.Id}", new ImportResponse(import.Id, import.Status));
    }

    private static async Task<byte[]> ReadContent(IFormFile file, CancellationToken ct)
    {
        await using var input = file.OpenReadStream();
        using var content = new MemoryStream();
        await input.CopyToAsync(content, ct);
        return content.ToArray();
    }

    internal static ImportSource? FindSource(IReadOnlyList<ImportSource> sources, string? entryPath, int? entryIndex) =>
        entryIndex is not null
            ? sources.FirstOrDefault(x => x.EntryIndex == entryIndex)
            : entryPath is null ? sources.FirstOrDefault() : sources.FirstOrDefault(x => x.Path == entryPath);

    private static async Task<ImportPlan> ResolvePlan(string template, BancosDbContext db, CancellationToken ct)
    {
        var metadata = ImportReviewTemplates.Get(template);
        if (metadata is null) return new(null, "No pudimos identificar el tipo de archivo. Selecciona uno de los tipos disponibles.");
        if (!metadata.IsEnabled) return new(null, $"Identificamos {metadata.Label}, pero su extractor todavía no está disponible.");

        var candidates = await db.AccountAuxiliaries.AsNoTracking()
            .Where(x => x.Account!.Kind == metadata.AccountKind)
            .Select(x => x.Id)
            .Take(2)
            .ToListAsync(ct);
        return candidates.Count switch
        {
            1 => new(candidates[0], null),
            0 => new(null, "No existe un auxiliar compatible para este tipo de archivo."),
            _ => new(null, "Hay más de un auxiliar compatible; se requiere una selección adicional.")
        };
    }

    private static ImportPreviewResponse ToPreviewResponse(ImportTemplateDetection detection, ImportPlan plan)
    {
        var metadata = ImportReviewTemplates.Get(detection.Template);
        var status = plan.Error is null ? "ready" : metadata is null ? "needs-type" : "unsupported";
        return new ImportPreviewResponse(detection.Template, metadata?.Label ?? "Tipo sin identificar", status, plan.Error);
    }

    private static async Task<Ok<List<ImportDetailResponse>>> List(BancosDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Imports.AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new ImportDetailResponse(x.Id, x.AccountAuxiliaryId, x.FileName, x.Status, x.Template, x.FailureReason, x.ProcessedUtc,
                db.ImportProgress.Where(progress => progress.ImportId == x.Id)
                    .Select(progress => new ImportProgressResponse(progress.ImportId, progress.Attempt, progress.Stage, progress.Current, progress.Total, progress.Percent, progress.Status.ToString(), progress.UpdatedUtc))
                    .SingleOrDefault()))
            .ToListAsync(ct));

    private static async Task<Results<Ok<ImportDetailResponse>, NotFound>> Get(Guid id, BancosDbContext db, CancellationToken ct)
    {
        var import = await db.Imports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return import is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new ImportDetailResponse(import.Id, import.AccountAuxiliaryId, import.FileName, import.Status, import.Template, import.FailureReason, import.ProcessedUtc,
                await db.ImportProgress.AsNoTracking().Where(x => x.ImportId == id)
                    .Select(x => new ImportProgressResponse(x.ImportId, x.Attempt, x.Stage, x.Current, x.Total, x.Percent, x.Status.ToString(), x.UpdatedUtc))
                    .SingleOrDefaultAsync(ct)));
    }

    private static async Task<Results<Ok<ImportResponse>, NotFound>> Retry(Guid id, BancosDbContext db, IImportJobScheduler scheduler, CancellationToken ct)
    {
        var import = await db.Imports.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (import is null) return TypedResults.NotFound();
        import.Status = ImportStatus.Queued; import.FailureReason = null;
        await db.SaveChangesAsync(ct);
        scheduler.Enqueue(import.Id);
        return TypedResults.Ok(new ImportResponse(import.Id, import.Status));
    }

    private static async Task<Results<Ok<ImportProgressResponse>, NotFound>> GetProgress(Guid id, BancosDbContext db, CancellationToken ct)
    {
        var progress = await db.ImportProgress.AsNoTracking().Where(x => x.ImportId == id)
            .Select(x => new ImportProgressResponse(x.ImportId, x.Attempt, x.Stage, x.Current, x.Total, x.Percent, x.Status.ToString(), x.UpdatedUtc))
            .SingleOrDefaultAsync(ct);
        return progress is null ? TypedResults.NotFound() : TypedResults.Ok(progress);
    }
}
public sealed record ImportResponse(Guid Id, ImportStatus Status);
public sealed record ImportDetailResponse(Guid Id, Guid AccountAuxiliaryId, string FileName, ImportStatus Status, string? Template, string? FailureReason, DateTime? ProcessedUtc, ImportProgressResponse? Progress);
public sealed record ImportPreviewResponse(string Template, string Label, string Status, string? Message);
public sealed record ImportPreviewEntryResponse(int EntryIndex, string Path, ImportPreviewResponse Preview);
public sealed record ImportPreviewBatchResponse(IReadOnlyList<ImportPreviewEntryResponse> Entries);
internal sealed record ImportPlan(Guid? AccountAuxiliaryId, string? Error);

internal sealed record ImportReviewTemplate(string Template, string Label, AccountKind AccountKind, bool IsEnabled);
internal static class ImportReviewTemplates
{
    private static readonly IReadOnlyDictionary<string, ImportReviewTemplate> Values = new Dictionary<string, ImportReviewTemplate>
    {
        [ImportTemplates.BcrDebitCsvV1] = new(ImportTemplates.BcrDebitCsvV1, "Movimientos de cuenta", AccountKind.Asset, true),
        [ImportTemplates.BacCreditFinancingXlsV1] = new(ImportTemplates.BacCreditFinancingXlsV1, "Financiamientos", AccountKind.Liability, true),
        [ImportTemplates.CoopealianzaLoanPdfV1] = new(ImportTemplates.CoopealianzaLoanPdfV1, "Estado de préstamo", AccountKind.Liability, true),
        [ImportTemplates.BacCreditCsvV1] = new(ImportTemplates.BacCreditCsvV1, "Estado de tarjeta", AccountKind.Liability, true),
        [ImportTemplates.BcrDebitHtmlXlsV1] = new(ImportTemplates.BcrDebitHtmlXlsV1, "Movimientos de cuenta", AccountKind.Asset, true),
        [ImportTemplates.BankAccountMovementsXlsV1] = new(ImportTemplates.BankAccountMovementsXlsV1, "Movimientos de cuenta (Excel)", AccountKind.Asset, true),
        [ImportTemplates.BacCreditOnlinePdfV1] = new(ImportTemplates.BacCreditOnlinePdfV1, "Estado de tarjeta", AccountKind.Liability, true),
        [ImportTemplates.BacAccountStatementPdfV1] = new(ImportTemplates.BacAccountStatementPdfV1, "Estado de cuenta consolidado BAC", AccountKind.Liability, true),
        [ImportTemplates.BnCardStatementPdfV1] = new(ImportTemplates.BnCardStatementPdfV1, "Estado de tarjeta Banco Nacional", AccountKind.Liability, true)
    };

    public static ImportReviewTemplate? Get(string template) => Values.GetValueOrDefault(template);
    public static IReadOnlyList<ImportReviewTemplate> Enabled { get; } = Values.Values.Where(x => x.IsEnabled).ToArray();
}
public interface IImportJobScheduler { void Enqueue(Guid importId); }
public sealed class HangfireImportJobScheduler(IBackgroundJobClient jobs) : IImportJobScheduler
{
    public void Enqueue(Guid importId) => jobs.Enqueue<ImportJobs>(x => x.ProcessAsync(importId, null!));
}
