using System.ComponentModel.DataAnnotations;

namespace Bancos.Mcp.Features.TemplateDetection;

public sealed class FileTemplateDetectionOptions
{
    public const string Section = "FileTemplateDetection";

    [Required, MinLength(1)]
    public string InputDirectory { get; init; } = "../input";

    [Range(1, 104_857_600)]
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    [Range(1, 100_000)]
    public int MaxSpreadsheetRows { get; init; } = 10_000;

    [Range(1, 1_000_000)]
    public int MaxSpreadsheetCells { get; init; } = 100_000;

    [Range(1, 10_000_000)]
    public int MaxExtractedCharacters { get; init; } = 1_000_000;
}
