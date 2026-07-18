using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Infrastructure;
using Bancos.Api.Features.Parsing;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Bancos.Api.Features.Imports;

public static class ImportsModule
{
    public static IServiceCollection AddImportsModule(this IServiceCollection services)
    {
        services.AddScoped<ImportJobs>(); services.AddScoped<BacCreditFinancingXlsParser>(); services.AddScoped<CoopealianzaLoanPdfParser>();
        services.AddScoped<IImportJobScheduler, HangfireImportJobScheduler>();
        services.AddSingleton<ImportTemplateDetector>();
        services.AddSingleton<BcrDebitCsvParser>();
        return services;
    }
    public static IEndpointRouteBuilder MapImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports");
        group.MapPost("/preview", Preview).DisableAntiforgery();
        group.MapPost("/upload", Upload).DisableAntiforgery();
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        return app;
    }
    private static async Task<Ok<ImportPreviewBatchResponse>> Preview(IFormFile file, BancosDbContext db, ImportTemplateDetector detector, CancellationToken ct)
    {
        var sources = ZipImportReader.Read(file.FileName, await ReadContent(file, ct));
        var entries = new List<ImportPreviewEntryResponse>();
        foreach (var source in sources)
        {
            try { var detection = detector.Detect(source.Content); var plan = await ResolvePlan(detection.Template, db, ct); entries.Add(new(source.Path, ToPreviewResponse(detection, plan))); }
            catch (Exception) when (source.Content.Length > 0) { entries.Add(new(source.Path, new(ImportTemplates.Unknown, "No se pudo analizar", "unsupported", "Este archivo interno no se puede procesar."))); }
        }
        return TypedResults.Ok(new ImportPreviewBatchResponse(entries));
    }

    private static async Task<Results<Created<ImportResponse>, ValidationProblem>> Upload(IFormFile file, string? template, BancosDbContext db, ImportTemplateDetector detector, IImportJobScheduler scheduler, IOptions<StorageOptions> storage, CancellationToken ct)
    {
        if (file.Length == 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["El archivo no puede estar vacío."] });
        var detection = detector.Detect(await ReadContent(file, ct));
        var selectedTemplate = string.IsNullOrWhiteSpace(template) ? detection.Template : template;
        var plan = await ResolvePlan(selectedTemplate, db, ct);
        if (plan.Error is not null) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["template"] = [plan.Error] });

        Directory.CreateDirectory(storage.Value.TemporaryPath); var path = Path.Combine(storage.Value.TemporaryPath, $"{Guid.NewGuid():N}.upload");
        await using (var destination = File.Create(path)) await file.CopyToAsync(destination, ct);
        string hash; await using (var input = File.OpenRead(path)) hash = Convert.ToHexString(await SHA256.HashDataAsync(input, ct));
        if (await db.ImportFingerprints.AnyAsync(x => x.Hash == hash, ct)) { File.Delete(path); return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["An identical import already exists."] }); }
        var import = new Import { FileName = Path.GetFileName(file.FileName), TemporaryPath = path, ContentHash = hash, AccountAuxiliaryId = plan.AccountAuxiliaryId!.Value, Template = selectedTemplate };
        db.Imports.Add(import); db.ImportFingerprints.Add(new ImportFingerprint { Hash = hash, ImportId = import.Id }); await db.SaveChangesAsync(ct);
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
            .Select(x => new ImportDetailResponse(x.Id, x.AccountAuxiliaryId, x.FileName, x.Status, x.Template, x.FailureReason, x.ProcessedUtc))
            .ToListAsync(ct));

    private static async Task<Results<Ok<ImportDetailResponse>, NotFound>> Get(Guid id, BancosDbContext db, CancellationToken ct)
    {
        var import = await db.Imports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return import is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new ImportDetailResponse(import.Id, import.AccountAuxiliaryId, import.FileName, import.Status, import.Template, import.FailureReason, import.ProcessedUtc));
    }
}
public sealed record ImportResponse(Guid Id, ImportStatus Status);
public sealed record ImportDetailResponse(Guid Id, Guid AccountAuxiliaryId, string FileName, ImportStatus Status, string? Template, string? FailureReason, DateTime? ProcessedUtc);
public sealed record ImportPreviewResponse(string Template, string Label, string Status, string? Message);
public sealed record ImportPreviewEntryResponse(string Path, ImportPreviewResponse Preview);
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
        [ImportTemplates.BacCreditCsvV1] = new(ImportTemplates.BacCreditCsvV1, "Estado de tarjeta", AccountKind.Liability, false),
        [ImportTemplates.BcrDebitHtmlXlsV1] = new(ImportTemplates.BcrDebitHtmlXlsV1, "Movimientos de cuenta", AccountKind.Asset, false),
        [ImportTemplates.BacCreditOnlinePdfV1] = new(ImportTemplates.BacCreditOnlinePdfV1, "Estado de tarjeta", AccountKind.Liability, false)
    };

    public static ImportReviewTemplate? Get(string template) => Values.GetValueOrDefault(template);
    public static IReadOnlyList<ImportReviewTemplate> Enabled { get; } = Values.Values.Where(x => x.IsEnabled).ToArray();
}
public interface IImportJobScheduler { void Enqueue(Guid importId); }
public sealed class HangfireImportJobScheduler(IBackgroundJobClient jobs) : IImportJobScheduler
{
    public void Enqueue(Guid importId) => jobs.Enqueue<ImportJobs>(x => x.ProcessAsync(importId, null!));
}
