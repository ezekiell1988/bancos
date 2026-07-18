using System.Globalization;

namespace Bancos.Api.Features.Parsing;

public sealed record ParsedBankMovement(DateOnly BookingDate, string ExternalReference, string Description, decimal Debit, decimal Credit);

public sealed class BcrDebitCsvParser
{
    private static readonly string[] RequiredHeaders = ["oficina", "fechamovimiento", "numerodocumento", "debito", "credito", "descripcion"];

    public IReadOnlyList<ParsedBankMovement> Parse(string csv)
    {
        var rows = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length < 2) throw new InvalidDataException("The BCR file has no movement rows.");
        var header = Split(rows[0]);
        var columns = RequiredHeaders.ToDictionary(name => name, name => Array.FindIndex(header, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)));
        if (columns.Any(pair => pair.Value < 0)) throw new InvalidDataException("The BCR file is missing a required header.");

        var movements = new List<ParsedBankMovement>();
        foreach (var row in rows.Skip(1))
        {
            var values = Split(row);
            if (values.Length != header.Length) throw new InvalidDataException("A BCR movement row has an invalid column count.");
            var debit = ParseAmount(values[columns["debito"]]);
            var credit = ParseAmount(values[columns["credito"]]);
            if ((debit == 0m) == (credit == 0m)) throw new InvalidDataException("Every BCR movement must have exactly one direction.");
            if (!TryParseDate(values[columns["fechamovimiento"]], out var bookingDate)) throw new InvalidDataException("A BCR movement has an invalid date.");
            movements.Add(new ParsedBankMovement(bookingDate, values[columns["numerodocumento"]], values[columns["descripcion"]], debit, credit));
        }
        ValidateBalances(header, rows.Skip(1).Select(Split).ToArray(), movements);
        return movements;
    }

    private static void ValidateBalances(string[] header, string[][] rows, IReadOnlyCollection<ParsedBankMovement> movements)
    {
        var openingIndex = Array.FindIndex(header, value => string.Equals(value, "saldoInicial", StringComparison.OrdinalIgnoreCase));
        var closingIndex = Array.FindIndex(header, value => string.Equals(value, "saldoFinal", StringComparison.OrdinalIgnoreCase));
        if (openingIndex < 0 || closingIndex < 0) return;
        var opening = rows.Select(row => row[openingIndex]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var closing = rows.Select(row => row[closingIndex]).LastOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (opening is null || closing is null) return;
        if (ParseAmount(opening) + movements.Sum(movement => movement.Credit - movement.Debit) != ParseAmount(closing))
            throw new InvalidDataException("The BCR opening balance, movements, and closing balance do not reconcile.");
    }

    private static string[] Split(string row) => row.Split(';').Select(value => value.Trim().Trim('"')).ToArray();
    private static decimal ParseAmount(string value) => string.IsNullOrWhiteSpace(value) ? 0m : decimal.Parse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("es-CR"));
    private static bool TryParseDate(string value, out DateOnly date) => DateOnly.TryParse(value, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.None, out date) || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
