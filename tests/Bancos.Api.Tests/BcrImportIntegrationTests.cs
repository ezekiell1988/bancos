using System.Net;
using System.Net.Http.Json;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Accounts;
using Bancos.Api.Features.Imports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class BcrImportIntegrationTests : IClassFixture<BancosApiFactory>
{
    private readonly BancosApiFactory _factory;

    public BcrImportIntegrationTests(BancosApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_then_job_persists_anonimized_bcr_movements_idempotently()
    {
        var client = _factory.CreateClient();
        var owner = await CreateAsync<CreateOwnerRequest, OwnerResponse>(client, "/api/accounts/owners", new("Fixture owner", null));
        var account = await CreateAsync<CreateAccountRequest, AccountResponse>(client, "/api/accounts", new("1100", "Fixture account", AccountKind.Asset));
        var auxiliary = await CreateAsync<CreateAccountAuxiliaryRequest, AccountAuxiliaryResponse>(client, "/api/accounts/auxiliaries", new("Fixture auxiliary", null, owner.Id, account.Id));

        var created = await UploadAsync(client, auxiliary.Id);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var import = await created.Content.ReadFromJsonAsync<ImportResponse>();
        Assert.NotNull(import);
        Assert.Equal(import!.Id, Assert.Single(_factory.Scheduler.EnqueuedImportIds));

        await ProcessAsync(import.Id);
        await ProcessAsync(import.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancosDbContext>();
        Assert.Equal(2, await db.Transactions.CountAsync());
        Assert.Equal(ImportStatus.Completed, (await db.Imports.SingleAsync(x => x.Id == import.Id)).Status);
        var progress = await db.ImportProgress.SingleAsync(x => x.ImportId == import.Id);
        Assert.Equal(ImportStatus.Completed, progress.Status);
        Assert.Equal(100, progress.Percent);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.EntityName == nameof(Import) && log.Action == nameof(EntityState.Modified));

        var progressResponse = await client.GetAsync($"/api/imports/{import.Id}/progress");
        progressResponse.EnsureSuccessStatusCode();
        var snapshot = await progressResponse.Content.ReadFromJsonAsync<ImportProgressResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal(import.Id, snapshot!.ImportId);
        Assert.Equal(nameof(ImportStatus.Completed), snapshot.Status);
        Assert.Equal(100, snapshot.Percent);
    }

    private static async Task<TResponse> CreateAsync<TRequest, TResponse>(HttpClient client, string path, TRequest request)
    {
        var response = await client.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid auxiliaryId)
    {
        var csv = "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion\n001;2026-01-02;FIX-001;0;1000;Movimiento anonimizado uno\n001;2026-01-03;FIX-002;250;0;Movimiento anonimizado dos";
        var content = new MultipartFormDataContent { { new StringContent(csv), "file", "bcr-fixture.csv" } };
        return await client.PostAsync($"/api/imports/upload?accountAuxiliaryId={auxiliaryId}", content);
    }

    private async Task ProcessAsync(Guid importId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ImportJobs>().ProcessAsync(importId, null);
    }
}

public sealed class BancosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    public RecordingImportJobScheduler Scheduler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<BancosDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IImportJobScheduler>();
            services.AddSingleton<IImportJobScheduler>(Scheduler);
        });
    }
}

public sealed class RecordingImportJobScheduler : IImportJobScheduler
{
    public List<Guid> EnqueuedImportIds { get; } = [];
    public void Enqueue(Guid importId) => EnqueuedImportIds.Add(importId);
}
