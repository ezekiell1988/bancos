using System.Security.Cryptography;
using System.Text;
using Bancos.Mcp.Features.Parsing;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class BnCardStatementPdfParserTests
{
    [Fact]
    public void Extracts_one_shared_identity_from_repeated_account_fields()
    {
        var iban = CreateSyntheticIban("000000000000000001");
        const string maskedCard = "************0000";

        var identity = BnCardStatementPdfParser.ExtractIdentityFingerprintsFromText($"""
            Cuenta IBAN {iban}
            Número de cuenta {maskedCard}
            Cuenta IBAN {iban}
            """);

        Assert.Equal(Hash(iban), identity.IdentifierHash);
        Assert.Equal(Hash(maskedCard), identity.CardFingerprint);
    }

    [Fact]
    public void Rejects_conflicting_account_identities()
    {
        var firstIban = CreateSyntheticIban("000000000000000001");
        var secondIban = CreateSyntheticIban("000000000000000002");

        Assert.Throws<InvalidDataException>(
            () => BnCardStatementPdfParser.ExtractIdentityFingerprintsFromText($"""
                Cuenta IBAN {firstIban}
                Número de cuenta ************0000
                Cuenta IBAN {secondIban}
                """));
    }

    [Fact]
    public void Rejects_invalid_account_identity()
    {
        Assert.Throws<InvalidDataException>(
            () => BnCardStatementPdfParser.ExtractIdentityFingerprintsFromText("""
                Cuenta IBAN CR00000000000000000000
                Número de cuenta ************0000
                """));
    }

    [Fact]
    public void Rejects_invalid_identity_even_when_another_occurrence_is_valid()
    {
        var validIban = CreateSyntheticIban("000000000000000001");

        Assert.Throws<InvalidDataException>(
            () => BnCardStatementPdfParser.ExtractIdentityFingerprintsFromText($"""
                Cuenta IBAN {validIban}
                Número de cuenta ************0000
                Cuenta IBAN CR00000000000000000000
                """));
    }

    [Fact]
    public void Parses_reconstructed_text_with_spaced_labels_columns_and_sections()
    {
        var parsed = BnCardStatementPdfParser.ParseText("""
            BANCO NACIONAL DE COSTA RICA
            ESTADO DE CUENTA TARJETAS DE CRÉDITO
            Marca de la tarjeta VISA
            Número de cuenta ****0000
            Plan de Lealtad PUNTOS
            Cuenta IBAN
            Fecha de emisión y corte 18/07/2026
            Fecha límite de pago de contado 03/08/2026
            Saldo anterior 80.00 0.00 5.00 0.00
            Saldo al corte 85.00 0.00 10.00 0.00
            TOTAL PAGO MÍNIMO 10.00 1.00 TOTAL PAGO DE CONTADO 100.00 10.00
            DETALLE DE PAGOS Y CRÉDITOS DEL PERÍODO
            01/07/2026 DB CTA 1000 -20.00 0.00 0.00 0.00
            TOTAL PAGOS RECIBIDOS -20.00 0.00 0.00 0.00
            DETALLE DE COMPRAS DEL PERÍODO
            02/07/2026 COMERCIO DE PRUEBA 25.00 0.00
            03/07/2026 SERVICIO DE PRUEBA 0.00 5.00
            TOTAL DE COMPRAS DEL PERÍODO 25.00 5.00
            """);

        Assert.Equal(new DateOnly(2026, 7, 18), parsed.StatementDate);
        Assert.Equal(80m, parsed.PreviousBalanceCrc);
        Assert.Equal(5m, parsed.PreviousBalanceUsd);
        Assert.Equal(85m, parsed.CurrentBalanceCrc);
        Assert.Equal(10m, parsed.CurrentBalanceUsd);
        Assert.Equal(10m, parsed.MinimumPaymentCrc);
        Assert.Equal(1m, parsed.MinimumPaymentUsd);
        Assert.Equal(100m, parsed.CashPaymentCrc);
        Assert.Equal(10m, parsed.CashPaymentUsd);
        Assert.Collection(
            parsed.Movements,
            movement => Assert.Equal(CardOperationKind.Payment, movement.Operation),
            movement =>
            {
                Assert.Equal(CardOperationKind.Purchase, movement.Operation);
                Assert.Equal("CRC", movement.OriginalCurrencyCode);
            },
            movement =>
            {
                Assert.Equal(CardOperationKind.Purchase, movement.Operation);
                Assert.Equal("USD", movement.OriginalCurrencyCode);
            });
        Assert.Equal(
            parsed.CurrentBalanceCrc,
            parsed.PreviousBalanceCrc + parsed.Movements
                .Where(movement => movement.OriginalCurrencyCode == "CRC")
                .Sum(SignedForBalance));
        Assert.Equal(
            parsed.CurrentBalanceUsd,
            parsed.PreviousBalanceUsd + parsed.Movements
                .Where(movement => movement.OriginalCurrencyCode == "USD")
                .Sum(SignedForBalance));
        Assert.Equal(0.01m, BnCardStatementPdfParser.BalanceTolerance);
    }

    private static decimal SignedForBalance(ParsedCardMovement movement) =>
        movement.Operation == CardOperationKind.Payment
            ? -Math.Abs(movement.OriginalAmount)
            : Math.Abs(movement.OriginalAmount);

    private static string CreateSyntheticIban(string bban)
    {
        var provisional = $"CR00{bban}";
        var remainder = Mod97(string.Concat(provisional.AsSpan(4), provisional.AsSpan(0, 4)));
        return $"CR{98 - remainder:00}{bban}";
    }

    private static int Mod97(string value)
    {
        var remainder = 0;
        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                remainder = ((remainder * 10) + (character - '0')) % 97;
                continue;
            }

            remainder = ((remainder * 100) + character - 'A' + 10) % 97;
        }

        return remainder;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
