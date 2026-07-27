using System.ComponentModel.DataAnnotations;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed class BccrOptions
{
    public const string Section = "Bccr";
    public const string DefaultBaseUrl = "https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API/";

    public string? Token { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    [Range(1, int.MaxValue)]
    public int DollarIndicator { get; init; } = 318;

    public string[] BankCodes { get; init; } = ["BN", "BAC"];

    [Range(1, 60)]
    public int RequestTimeoutSeconds { get; init; } = 30;
}
