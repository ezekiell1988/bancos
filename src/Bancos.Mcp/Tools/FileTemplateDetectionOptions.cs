using System.ComponentModel.DataAnnotations;

namespace Bancos.Mcp.Tools;

public sealed class FileTemplateDetectionOptions
{
    public const string Section = "FileTemplateDetection";

    [Required, MinLength(1)]
    public string InputDirectory { get; init; } = "../input";

    [Range(1, 104_857_600)]
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
}