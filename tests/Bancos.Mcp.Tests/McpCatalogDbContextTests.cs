using Bancos.Mcp.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class McpCatalogDbContextTests
{
    [Fact]
    public async Task Import_templates_seed_the_known_api_formats()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var templates = await db.ImportTemplates.OrderBy(template => template.Code).ToListAsync();
        var patterns = await db.ImportTemplatePatterns.OrderBy(pattern => pattern.TemplateId).ToListAsync();

        Assert.Equal(9, templates.Count);
        Assert.All(templates, template => Assert.True(template.IsEnabled));
        Assert.All(templates, template => Assert.Equal(TimeSpan.FromHours(-6), template.CreatedAt.Offset));
        Assert.Equal(9, patterns.Count);
        Assert.All(patterns, pattern => Assert.True(pattern.IsApproved && pattern.IsActive));
        Assert.All(patterns, pattern => Assert.Equal("content-terms", pattern.PatternKind));
        Assert.All(patterns, pattern => Assert.Equal(TimeSpan.FromHours(-6), pattern.CreatedAt.Offset));
        Assert.Contains(templates, template => template.Code == "bcr-debit-csv-v1" && template.ContentKind == "csv");
        Assert.Contains(templates, template => template.Code == "bn-card-statement-pdf-v1" && template.ContentKind == "pdf");
    }
}