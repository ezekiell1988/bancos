using System.ComponentModel.DataAnnotations;

namespace Bancos.Api.Infrastructure;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    [Required, MinLength(1)] public required string TemporaryPath { get; init; }
}

