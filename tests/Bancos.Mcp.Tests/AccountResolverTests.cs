using System.Security.Cryptography;
using System.Text;
using Bancos.Mcp.Catalog;
using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.FileProcessing;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class AccountResolverTests
{
    [Fact]
    public void Resolves_bn_card_pair_with_shared_identity_and_distinct_currencies()
    {
        var crcAccountId = Guid.NewGuid();
        var usdAccountId = Guid.NewGuid();
        var identity = new BnCardStatementIdentity(new string('A', 64), new string('B', 64));
        BnCardAccountCandidate[] accounts =
        [
            new(crcAccountId, "CRC", identity.IdentifierHash, identity.CardFingerprint),
            new(usdAccountId, "USD", identity.IdentifierHash, identity.CardFingerprint)
        ];

        var resolved = AccountResolver.ResolveBnCardStatementPair(accounts, identity);

        Assert.Equal(crcAccountId, resolved.CrcAccountId);
        Assert.Equal(usdAccountId, resolved.UsdAccountId);
    }

    [Fact]
    public void Rejects_bn_card_pair_when_a_registered_fingerprint_is_missing()
    {
        var identity = new BnCardStatementIdentity(new string('A', 64), new string('B', 64));
        BnCardAccountCandidate[] accounts =
        [
            new(Guid.NewGuid(), "CRC", identity.IdentifierHash, identity.CardFingerprint),
            new(Guid.NewGuid(), "USD", identity.IdentifierHash, null)
        ];

        var error = Assert.Throws<InvalidOperationException>(
            () => AccountResolver.ResolveBnCardStatementPair(accounts, identity));

        Assert.DoesNotContain(identity.IdentifierHash, error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(identity.CardFingerprint, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_bn_card_pair_when_document_identity_does_not_match()
    {
        var identity = new BnCardStatementIdentity(new string('A', 64), new string('B', 64));
        BnCardAccountCandidate[] accounts =
        [
            new(Guid.NewGuid(), "CRC", new string('C', 64), identity.CardFingerprint),
            new(Guid.NewGuid(), "USD", new string('C', 64), identity.CardFingerprint)
        ];

        Assert.Throws<InvalidOperationException>(
            () => AccountResolver.ResolveBnCardStatementPair(accounts, identity));
    }

    [Fact]
    public async Task Resolves_account_from_parent_folder_without_changing_detected_template()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string testIban = "CR00000000000000000000";
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = (await db.Banks.FirstAsync()).Id,
            Code = $"test-debit-{Guid.NewGuid():N}",
            IdentifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(testIban))),
            AccountType = "debit-card",
            CurrencyCode = "CRC"
        };
        var templateId = ImportTemplateCatalog.Definitions
            .Single(definition => definition.ParserKey == "bank-account-movements-xls")
            .Id;
        db.BankAccounts.Add(account);
        db.BankAccountImportTemplates.Add(new BankAccountImportTemplate
        {
            BankAccountId = account.Id,
            ImportTemplateId = templateId
        });
        await db.SaveChangesAsync();

        var resolved = await new AccountResolver(db).ResolveLinkedAccountByIbanPathAsync(
            $"20260717/DEBITO/{testIban}/statement.xls", templateId, CancellationToken.None);

        Assert.Equal(account.Id, resolved);
    }

    [Fact]
    public async Task Resolves_debit_csv_variant_from_parent_folder_and_excludes_xls_template()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string testIban = "CR11111111111111111111";
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = (await db.Banks.FirstAsync()).Id,
            Code = $"test-debit-csv-{Guid.NewGuid():N}",
            IdentifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(testIban))),
            AccountType = "debit-card",
            CurrencyCode = "USD"
        };
        var csvTemplateId = ImportTemplateCatalog.Definitions
            .Single(definition => definition.ParserKey == "bn-debit-csv")
            .Id;
        var xlsTemplateId = ImportTemplateCatalog.Definitions
            .Single(definition => definition.ParserKey == "bank-account-movements-xls")
            .Id;
        db.BankAccounts.Add(account);
        db.BankAccountImportTemplates.AddRange(
            new BankAccountImportTemplate
            {
                BankAccountId = account.Id,
                ImportTemplateId = csvTemplateId
            },
            new BankAccountImportTemplate
            {
                BankAccountId = account.Id,
                ImportTemplateId = xlsTemplateId
            });
        await db.SaveChangesAsync();

        var resolved = await new AccountResolver(db).TryResolveDebitCsvByIbanPathAsync(
            $"20260717/DEBITO/{testIban}/statement.csv", CancellationToken.None);

        Assert.NotNull(resolved);
        Assert.Equal(account.Id, resolved.Value.AccountId);
        Assert.Equal(csvTemplateId, resolved.Value.TemplateId);
        Assert.NotEqual(xlsTemplateId, resolved.Value.TemplateId);
    }
}
