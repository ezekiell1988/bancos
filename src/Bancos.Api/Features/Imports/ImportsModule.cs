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
        services.AddScoped<ImportJobs>();
        services.AddSingleton<ImportTemplateDetector>();
        services.AddSingleton<BcrDebitCsvParser>();
        return services;
    }
    public static IEndpointRouteBuilder MapImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports");
        group.MapPost("/upload", Upload).DisableAntiforgery();
        return app;
    }
    private static async Task<Results<Created<ImportResponse>, ValidationProblem>> Upload(IFormFile file, Guid accountAuxiliaryId, BancosDbContext db, IBackgroundJobClient jobs, IOptions<StorageOptions> storage, CancellationToken ct)
    {
        if (file.Length == 0) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["The file must not be empty."] });
        if (!await db.AccountAuxiliaries.AnyAsync(x => x.Id == accountAuxiliaryId, ct)) return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["accountAuxiliaryId"] = ["The account auxiliary was not found."] });
        Directory.CreateDirectory(storage.Value.TemporaryPath); var path = Path.Combine(storage.Value.TemporaryPath, $"{Guid.NewGuid():N}.upload");
        await using (var destination = File.Create(path)) await file.CopyToAsync(destination, ct);
        string hash; await using (var input = File.OpenRead(path)) hash = Convert.ToHexString(await SHA256.HashDataAsync(input, ct));
        if (await db.ImportFingerprints.AnyAsync(x => x.Hash == hash, ct)) { File.Delete(path); return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["An identical import already exists."] }); }
        var import = new Import { FileName = Path.GetFileName(file.FileName), TemporaryPath = path, ContentHash = hash, AccountAuxiliaryId = accountAuxiliaryId };
        db.Imports.Add(import); db.ImportFingerprints.Add(new ImportFingerprint { Hash = hash, ImportId = import.Id }); await db.SaveChangesAsync(ct);
        jobs.Enqueue<ImportJobs>(x => x.ProcessAsync(import.Id, null!));
        return TypedResults.Created($"/api/imports/{import.Id}", new ImportResponse(import.Id, import.Status));
    }
}
public sealed record ImportResponse(Guid Id, ImportStatus Status);
