using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ClassificationServiceTests
{
    private static readonly Guid GroceriesCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000008");
    private static readonly Guid TransportCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000009");
    private const string GroceriesCategoryCode = "expense.groceries";

    internal static ClassificationService CreateService(McpCatalogDbContext db, ClassificationAiOptions? aiOptions = null) =>
        new(db, new AzureAiClassifier(new HttpClient(), Options.Create(aiOptions ?? new ClassificationAiOptions { Enabled = false })), Options.Create(aiOptions ?? new ClassificationAiOptions { Enabled = false }));

    private static ClassificationService CreateServiceWithSimulatedAi(
        McpCatalogDbContext db,
        FakeAiHttpMessageHandler handler,
        double minimumConfidence = 0.8)
    {
        var aiOptions = new ClassificationAiOptions
        {
            Enabled = true,
            Endpoint = "https://fake-azure-ai.local/openai/v1",
            ApiKey = "test-key",
            Model = "gpt-5",
            MinimumConfidence = minimumConfidence
        };
        var classifier = new AzureAiClassifier(new HttpClient(handler), Options.Create(aiOptions));
        return new ClassificationService(db, classifier, Options.Create(aiOptions));
    }

    [Fact]
    public async Task Classifies_by_ai_when_no_rule_matches_and_confidence_meets_threshold()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.RespondingWith(GroceriesCategoryCode, confidence: 0.9);
        var service = CreateServiceWithSimulatedAi(db, handler);

        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("ai", classification.Source);
        Assert.Equal(GroceriesCategoryId, classification.CategoryId);
        Assert.Equal(0.9m, classification.Confidence);
    }

    [Fact]
    public async Task Leaves_unclassified_when_ai_confidence_is_below_threshold()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.RespondingWith(GroceriesCategoryCode, confidence: 0.5);
        var service = CreateServiceWithSimulatedAi(db, handler, minimumConfidence: 0.8);

        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
        Assert.Null(classification.CategoryId);
    }

    [Fact]
    public async Task Leaves_unclassified_when_ai_call_fails()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.RespondingWithError(HttpStatusCode.ServiceUnavailable);
        var service = CreateServiceWithSimulatedAi(db, handler);

        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
    }

    [Fact]
    public async Task Leaves_unclassified_when_ai_request_times_out()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.Throwing(new TaskCanceledException("Tiempo de espera agotado."));
        var service = CreateServiceWithSimulatedAi(db, handler);

        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
        Assert.Contains("IA", classification.Explanation);
    }

    [Fact]
    public async Task Ai_prompt_only_contains_normalized_description_and_category_catalog()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.RespondingWith(GroceriesCategoryCode, confidence: 0.9);
        var service = CreateServiceWithSimulatedAi(db, handler);

        await service.ClassifyAsync(transactionId);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("compra automercado sabana", handler.LastRequestBody);
        Assert.Contains(GroceriesCategoryCode, handler.LastRequestBody);
        Assert.DoesNotContain(accountId.ToString(), handler.LastRequestBody);
        Assert.DoesNotContain("-10", handler.LastRequestBody);
    }

    [Fact]
    public async Task Ai_prompt_redacts_sensitive_identifiers_from_description()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        const string sensitiveDescription = "PAGO A PROVEEDOR cuenta CR12345678901234567890 tarjeta 4111 1111 1111 1111 correo persona@example.com telefono 88887777 por ₡125000";
        var transactionId = await AddTransactionAsync(db, accountId, sensitiveDescription);
        await db.SaveChangesAsync();

        var handler = FakeAiHttpMessageHandler.RespondingWith(GroceriesCategoryCode, confidence: 0.9);
        var service = CreateServiceWithSimulatedAi(db, handler);

        await service.ClassifyAsync(transactionId);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("pago a proveedor", handler.LastRequestBody);
        Assert.Contains("[iban]", handler.LastRequestBody);
        Assert.Contains("[identificador]", handler.LastRequestBody);
        Assert.Contains("[correo]", handler.LastRequestBody);
        Assert.Contains("[monto]", handler.LastRequestBody);
        Assert.DoesNotContain("cr12345678901234567890", handler.LastRequestBody);
        Assert.DoesNotContain("4111 1111 1111 1111", handler.LastRequestBody);
        Assert.DoesNotContain("persona@example.com", handler.LastRequestBody);
        Assert.DoesNotContain("88887777", handler.LastRequestBody);
        Assert.DoesNotContain("125000", handler.LastRequestBody);
    }

    private sealed class FakeAiHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responder;
        public string? LastRequestBody { get; private set; }

        private FakeAiHttpMessageHandler(Func<HttpResponseMessage> responder) => _responder = responder;

        public static FakeAiHttpMessageHandler RespondingWith(string categoryCode, double confidence) =>
            new(() =>
            {
                var content = JsonSerializer.Serialize(new { categoryCode, confidence, reasoning = "Coincide con el catálogo." });
                var body = new { choices = new[] { new { message = new { content } } } };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            });

        public static FakeAiHttpMessageHandler RespondingWithError(HttpStatusCode statusCode) =>
            new(() => new HttpResponseMessage(statusCode));

        public static FakeAiHttpMessageHandler Throwing(Exception exception) =>
            new(() => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _responder();
        }
    }

    [Fact]
    public async Task Classifies_by_rule_when_description_matches_and_records_origin_and_confidence()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "AUTOMERCADO",
            MatchType = "contains"
        });
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("rule", classification.Source);
        Assert.Equal(GroceriesCategoryId, classification.CategoryId);
        Assert.Equal(1m, classification.Confidence);
        Assert.NotNull(classification.ClassificationRuleId);
        Assert.NotEmpty(classification.Explanation!);
    }

    [Fact]
    public async Task Leaves_unclassified_with_no_category_when_no_rule_matches()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
        Assert.Null(classification.CategoryId);
        Assert.Null(classification.ClassificationRuleId);
        Assert.Null(classification.Confidence);
    }

    [Fact]
    public async Task Prefers_the_more_specific_rule_when_several_match()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.AddRange(
            new ClassificationRule
            {
                Id = Guid.NewGuid(),
                CategoryId = TransportCategoryId,
                DescriptionPattern = "UBER",
                MatchType = "contains"
            },
            new ClassificationRule
            {
                Id = Guid.NewGuid(),
                CategoryId = GroceriesCategoryId,
                BankAccountId = accountId,
                DescriptionPattern = "UBER",
                MatchType = "contains"
            });
        var transactionId = await AddTransactionAsync(db, accountId, "UBER TRIP 123");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal(GroceriesCategoryId, classification.CategoryId);
    }

    [Fact]
    public async Task Ignores_disabled_rules()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "AUTOMERCADO",
            MatchType = "contains",
            IsEnabled = false
        });
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
    }

    [Fact]
    public async Task Keeps_history_of_every_classification_attempt()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.ClassifyAsync(transactionId);
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "COMPRA SIN REGLA",
            MatchType = "exact"
        });
        await db.SaveChangesAsync();
        await service.ClassifyAsync(transactionId);

        var history = await db.TransactionClassifications
            .Where(tc => tc.TransactionId == transactionId)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Contains(history, tc => tc.Source == "unclassified");
        Assert.Contains(history, tc => tc.Source == "rule" && tc.CategoryId == GroceriesCategoryId);
    }

    [Fact]
    public async Task ClassifyPendingAsync_only_processes_transactions_with_no_prior_attempt()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "AUTOMERCADO",
            MatchType = "contains"
        });
        var pendingRuleMatch = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        var pendingNoMatch = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        var alreadyAttempted = await AddTransactionAsync(db, accountId, "YA PROCESADO");
        await db.SaveChangesAsync();
        var service = CreateService(db);
        await service.ClassifyAsync(alreadyAttempted);

        // La cuenta ya trae un movimiento "Saldo inicial" sembrado sin clasificar (ver McpCatalogDbContext).
        var summary = await service.ClassifyPendingAsync(accountId, limit: 100);

        Assert.Equal(3, summary.Processed);
        Assert.Equal(1, summary.Rule);
        Assert.Equal(2, summary.Unclassified);
        var history = await db.TransactionClassifications.ToListAsync();
        Assert.Single(history, tc => tc.TransactionId == pendingRuleMatch);
        Assert.Single(history, tc => tc.TransactionId == pendingNoMatch);
        Assert.Single(history, tc => tc.TransactionId == alreadyAttempted);
    }

    [Fact]
    public async Task ListUnclassifiedAsync_excludes_transactions_later_classified()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var stillUnclassified = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA A");
        var laterConfirmed = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA B");
        await db.SaveChangesAsync();
        var service = CreateService(db);
        await service.ClassifyAsync(stillUnclassified);
        await service.ClassifyAsync(laterConfirmed);
        await service.ConfirmManualClassificationAsync(laterConfirmed, GroceriesCategoryId);

        // La cuenta ya trae un movimiento "Saldo inicial" sembrado sin clasificar (ver McpCatalogDbContext).
        var unclassified = await service.ListUnclassifiedAsync(accountId, page: 1, itemsPerPage: 50);

        Assert.Equal(2, unclassified.TotalItems);
        Assert.Contains(unclassified.Items, s => s.TransactionId == stillUnclassified);
        Assert.DoesNotContain(unclassified.Items, s => s.TransactionId == laterConfirmed);
        Assert.All(unclassified.Items, s => Assert.NotEmpty(s.Explanation));
    }

    [Fact]
    public async Task ListUnclassifiedAsync_returns_requested_page_and_toon_header()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        await AddTransactionAsync(db, accountId, "PENDIENTE UNO");
        await AddTransactionAsync(db, accountId, "PENDIENTE DOS");
        await AddTransactionAsync(db, accountId, "PENDIENTE TRES");

        var service = CreateService(db);
        var page = await service.ListUnclassifiedAsync(accountId, page: 2, itemsPerPage: 2);
        var toon = ToonFormatter.Format(page);

        Assert.Equal(4, page.TotalItems); // Incluye el movimiento inicial sembrado.
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains("format:toon", toon);
        Assert.Contains("page:2", toon);
        Assert.Contains("transactions[2]{transactionId,bankAccountId,transactionDate,description,amount,currencyCode,explanation}:", toon);
    }

    [Fact]
    public async Task ConfirmManualClassificationAsync_creates_reusable_rule_for_future_matches()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "PAGO NETFLIX");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var manual = await service.ConfirmManualClassificationAsync(transactionId, GroceriesCategoryId);

        Assert.Equal("manual", manual.Source);
        Assert.Equal(GroceriesCategoryId, manual.CategoryId);
        Assert.NotNull(manual.ClassificationRuleId);

        var laterTransactionId = await AddTransactionAsync(db, accountId, "PAGO NETFLIX");
        await db.SaveChangesAsync();
        var laterClassification = await service.ClassifyAsync(laterTransactionId);

        Assert.Equal("rule", laterClassification.Source);
        Assert.Equal(GroceriesCategoryId, laterClassification.CategoryId);
        Assert.Equal(manual.ClassificationRuleId, laterClassification.ClassificationRuleId);
    }

    [Fact]
    public async Task ConfirmManualClassificationAsync_updates_existing_rule_instead_of_duplicating()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var firstTransactionId = await AddTransactionAsync(db, accountId, "PAGO SPOTIFY");
        var secondTransactionId = await AddTransactionAsync(db, accountId, "PAGO SPOTIFY");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var first = await service.ConfirmManualClassificationAsync(firstTransactionId, TransportCategoryId);
        var second = await service.ConfirmManualClassificationAsync(secondTransactionId, GroceriesCategoryId);

        Assert.Equal(first.ClassificationRuleId, second.ClassificationRuleId);
        Assert.Equal(1, await db.ClassificationRules.CountAsync());
        var rule = await db.ClassificationRules.SingleAsync();
        Assert.Equal(GroceriesCategoryId, rule.CategoryId);
    }

    [Fact]
    public async Task ConfirmManualClassificationAsync_learns_and_propagates_place()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var firstTransactionId = await AddTransactionAsync(db, accountId, "COMPRA FERIA CENTRAL");
        var service = CreateService(db);

        await service.ConfirmManualClassificationAsync(firstTransactionId, GroceriesCategoryId, "Feria Central");

        var firstTransaction = await db.Transactions.FindAsync(firstTransactionId);
        var rule = await db.ClassificationRules.SingleAsync();
        Assert.Equal("Feria Central", firstTransaction!.Place);
        Assert.Equal("Feria Central", rule.Place);

        var futureTransactionId = await AddTransactionAsync(db, accountId, "COMPRA FERIA CENTRAL");
        await service.ClassifyAsync(futureTransactionId);

        var futureTransaction = await db.Transactions.FindAsync(futureTransactionId);
        Assert.Equal("Feria Central", futureTransaction!.Place);
    }

    private static async Task<McpCatalogDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<Guid> AddTransactionAsync(McpCatalogDbContext db, Guid accountId, string description)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = accountId,
            TransactionDate = new DateOnly(2026, 7, 20),
            Description = description,
            CurrencyCode = "CRC",
            Amount = -10m,
            AmountCrc = -10m,
            OperationType = "purchase",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction.Id;
    }
}
