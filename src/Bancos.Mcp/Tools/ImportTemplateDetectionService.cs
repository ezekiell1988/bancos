using System.Globalization;
using System.Text;
using ExcelDataReader;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace Bancos.Mcp.Tools;

public sealed class ImportTemplateDetectionService
{
    private static readonly HashSet<string> AllowedExtensions = [".csv", ".pdf", ".xls", ".xlsx"];
    private readonly string inputDirectory;
    private readonly long maxFileSizeBytes;

    public ImportTemplateDetectionService(IOptions<FileTemplateDetectionOptions> options, IHostEnvironment environment)
        : this(options.Value.InputDirectory, options.Value.MaxFileSizeBytes, environment.ContentRootPath)
    {
    }

    public ImportTemplateDetectionService(string configuredInputDirectory, long configuredMaxFileSizeBytes, string contentRootPath)
    {
        inputDirectory = Path.GetFullPath(configuredInputDirectory, contentRootPath);
        maxFileSizeBytes = configuredMaxFileSizeBytes;
    }

    public async Task<Guid> DetectAsync(string relativePath, CancellationToken cancellationToken)
    {
        var file = ResolveInputFile(relativePath);
        if (file.Length > maxFileSizeBytes)
            throw new ArgumentException("El archivo supera el tamaño máximo permitido.");

        var content = await File.ReadAllBytesAsync(file.FullName, cancellationToken);
        var contentKind = GetContentKind(file.Extension, content);
        var text = ExtractText(contentKind, content);
        return DetectTemplateId(contentKind, text);
    }

    private FileInfo ResolveInputFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("relativePath es requerido.");
        if (Path.IsPathRooted(relativePath) || ContainsTraversal(relativePath))
            throw new ArgumentException("La ruta debe ser relativa y permanecer dentro del directorio de entrada.");

        var extension = Path.GetExtension(relativePath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("El formato de archivo no está permitido.");

        var path = Path.GetFullPath(relativePath, inputDirectory);
        if (!path.StartsWith(inputDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !string.Equals(path, inputDirectory, StringComparison.Ordinal))
            throw new ArgumentException("La ruta debe permanecer dentro del directorio de entrada.");

        var file = new FileInfo(path);
        if (!file.Exists)
            throw new ArgumentException("El archivo solicitado no existe.");
        if (HasSymbolicLinkInPath(path))
            throw new ArgumentException("La ruta no puede contener enlaces simbólicos.");

        return file;
    }

    private bool HasSymbolicLinkInPath(string path)
    {
        var relativeSegments = Path.GetRelativePath(inputDirectory, path)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var current = inputDirectory;
        foreach (var segment in relativeSegments)
        {
            current = Path.Combine(current, segment);
            FileSystemInfo item = string.Equals(current, path, StringComparison.Ordinal)
                ? new FileInfo(current)
                : new DirectoryInfo(current);
            if (item.LinkTarget is not null)
                return true;
        }

        return false;
    }

    private static bool ContainsTraversal(string path) => path
        .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
        .Any(segment => segment == "..");

    private static string GetContentKind(string extension, byte[] content) => extension.ToLowerInvariant() switch
    {
        ".pdf" when content.AsSpan().StartsWith("%PDF-"u8) => "pdf",
        ".csv" when !IsBinaryOfficeDocument(content) && !content.AsSpan().StartsWith("%PDF-"u8) => "csv",
        ".xls" when IsHtml(content) => "html",
        ".xls" when IsLegacySpreadsheet(content) => "xls",
        ".xlsx" when IsOpenXmlSpreadsheet(content) => "xls",
        _ => throw new InvalidDataException("El contenido no coincide con el formato declarado.")
    };

    private static string ExtractText(string contentKind, byte[] content) => contentKind switch
    {
        "pdf" => ExtractPdf(content),
        "xls" => ExtractSpreadsheet(content),
        _ => Encoding.UTF8.GetString(content)
    };

    private static Guid DetectTemplateId(string contentKind, string text)
    {
        var normalized = Normalize(text);
        var matches = new List<Guid>();

        if (contentKind == "csv" && ContainsAll(normalized, ";", "oficina", "fechamovimiento", "numerodocumento", "debito", "credito", "descripcion"))
            matches.Add(TemplateIds.BcrDebitCsv);
        if (contentKind == "csv" && ContainsAll(normalized, ",", "name", "date", "minimum payment", "cash payment", "local amount") && HasAny(normalized, "dollar amount", "dollars amount"))
            matches.Add(TemplateIds.BacCreditCsv);
        if (contentKind == "html" && ContainsAll(normalized, "banco de costa rica") && HasAny(normalized, "movimientos por rango de fechas", "movimientos de la cuenta", "movimientos del d"))
            matches.Add(TemplateIds.BcrDebitHtmlXls);
        if (contentKind == "xls" && ContainsAll(normalized, "consulta de financiamientos", "fecha", "concepto", "cuotas", "monto de cuota", "saldo inicial", "saldo faltante"))
            matches.Add(TemplateIds.BacCreditFinancingXls);
        if (contentKind == "xls" && normalized.Contains("fecha") && HasAny(normalized, "descripcion", "detalle") && HasAny(normalized, "debito", "debitos") && HasAny(normalized, "credito", "creditos"))
            matches.Add(TemplateIds.BankAccountMovementsXls);
        if (contentKind == "pdf" && ContainsAll(normalized, "tarjeta de credito", "saldo en colones", "saldo en dolares", "fecha de pago de contado") && !normalized.Contains("total pago de contado"))
            matches.Add(TemplateIds.BacCreditOnlinePdf);
        if (contentKind == "pdf" && ContainsAll(normalized, "ver detalles del prestamo", "capital", "interes", "mora", "otros", "total", "saldo"))
            matches.Add(TemplateIds.CoopealianzaLoanPdf);
        if (contentKind == "pdf" && ContainsAll(normalized, "numero de tarjeta", "marca de tarjeta", "plan de lealtad", "pagos vencidos", "pago de contado", "fecha de corte", "total pago de contado"))
            matches.Add(TemplateIds.BacAccountStatementPdf);
        if (contentKind == "pdf" && ContainsAll(normalized, "banco nacional de costa rica", "estado de cuenta tarjetas de credito", "detalle de compras del periodo", "total pago de contado"))
            matches.Add(TemplateIds.BnCardStatementPdf);

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidDataException("No se encontró una plantilla de importación reconocida."),
            _ => throw new InvalidDataException("El archivo coincide con más de una plantilla de importación.")
        };
    }

    private static string ExtractPdf(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        return string.Join('\n', document.GetPages().Select(page => page.Text));
    }

    private static string ExtractSpreadsheet(byte[] content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(content);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var values = new List<string>();
        do
        {
            while (reader.Read())
                for (var column = 0; column < reader.FieldCount; column++)
                    if (reader.GetValue(column) is { } value)
                        values.Add(value.ToString()!);
        }
        while (reader.NextResult());

        return string.Join('\n', values);
    }

    private static bool IsLegacySpreadsheet(byte[] content) => content.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 });
    private static bool IsOpenXmlSpreadsheet(byte[] content) => content.AsSpan().StartsWith("PK"u8);
    private static bool IsBinaryOfficeDocument(byte[] content) => IsLegacySpreadsheet(content) || IsOpenXmlSpreadsheet(content);
    private static bool IsHtml(byte[] content) => Encoding.UTF8.GetString(content).Contains("<html", StringComparison.OrdinalIgnoreCase) || Encoding.UTF8.GetString(content).Contains("<table", StringComparison.OrdinalIgnoreCase);
    private static bool ContainsAll(string text, params string[] values) => values.All(text.Contains);
    private static bool HasAny(string text, params string[] values) => values.Any(text.Contains);

    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();

    private static class TemplateIds
    {
        public static readonly Guid BcrDebitCsv = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid BacCreditCsv = Guid.Parse("10000000-0000-0000-0000-000000000002");
        public static readonly Guid BcrDebitHtmlXls = Guid.Parse("10000000-0000-0000-0000-000000000003");
        public static readonly Guid BankAccountMovementsXls = Guid.Parse("10000000-0000-0000-0000-000000000004");
        public static readonly Guid BacCreditFinancingXls = Guid.Parse("10000000-0000-0000-0000-000000000005");
        public static readonly Guid BacCreditOnlinePdf = Guid.Parse("10000000-0000-0000-0000-000000000006");
        public static readonly Guid CoopealianzaLoanPdf = Guid.Parse("10000000-0000-0000-0000-000000000007");
        public static readonly Guid BacAccountStatementPdf = Guid.Parse("10000000-0000-0000-0000-000000000008");
        public static readonly Guid BnCardStatementPdf = Guid.Parse("10000000-0000-0000-0000-000000000009");
    }
}