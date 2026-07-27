using System.ComponentModel.DataAnnotations;

namespace Bancos.Mcp.Features.Classification;

public sealed class ClassificationAiOptions
{
    public const string Section = "ClassificationAi";

    public bool Enabled { get; init; }

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    [Required, MinLength(1)]
    public string Model { get; init; } = "gpt-5";

    [Range(0, 1)]
    public double MinimumConfidence { get; init; } = 0.8;
}
