using System.Globalization;
using System.Text;
using ExcelDataReader;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using Bancos.Mcp.Catalog;

namespace Bancos.Mcp.Features.TemplateDetection;

public sealed class ImportTemplateDetectionService
{
    private static readonly HashSet<string> AllowedExtensions = [".csv", ".pdf", ".xls", ".xlsx"];
    private readonly string inputDirectory;
    private readonly long maxFileSizeBytes;
    private readonly int maxSpreadsheetRows;
    private readonly int maxSpreadsheetCells;
    private readonly int maxExtractedCharacters;

    public ImportTemplateDetectionService(IOptions<FileTemplateDetectionOptions> options, IHostEnvironment environment)
        : this(options.Value.InputDirectory, options.Value.MaxFileSizeBytes, environment.ContentRootPath, options.Value.MaxSpreadsheetRows, options.Value.MaxSpreadsheetCells, options.Value.MaxExtractedCharacters)
    {
    }

    public ImportTemplateDetectionService(string configuredInputDirectory, long configuredMaxFileSizeBytes, string contentRootPath, int maxSpreadsheetRows = 10_000, int maxSpreadsheetCells = 100_000, int maxExtractedCharacters = 1_000_000)
    {
        inputDirectory = Path.GetFullPath(configuredInputDirectory, contentRootPath);
        maxFileSizeBytes = configuredMaxFileSizeBytes;
        this.maxSpreadsheetRows = maxSpreadsheetRows;
        this.maxSpreadsheetCells = maxSpreadsheetCells;
        this.maxExtractedCharacters = maxExtractedCharacters;
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

    private string ExtractText(string contentKind, byte[] content) => contentKind switch
    {
        "pdf" => ExtractPdf(content),
        "xls" => ExtractSpreadsheet(content),
        _ => ExtractPlainText(content)
    };

    private static Guid DetectTemplateId(string contentKind, string text)
    {
        var normalized = Normalize(text);
        var matches = ImportTemplateCatalog.Definitions
            .Where(definition => definition.ContentKind == contentKind)
            .Where(definition => ContainsAll(normalized, definition.RequiredTerms))
            .Where(definition => definition.AlternativeTermGroups is null || definition.AlternativeTermGroups.All(group => HasAny(normalized, group)))
            .Where(definition => definition.ExcludedTerms is null || !HasAny(normalized, definition.ExcludedTerms))
            .Select(definition => definition.Id)
            .ToList();

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

    private string ExtractPlainText(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        if (text.Length > maxExtractedCharacters)
            throw new InvalidDataException("El archivo supera los límites de extracción permitidos.");
        return text;
    }

    private string ExtractSpreadsheet(byte[] content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(content);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var values = new List<string>();
        var rows = 0;
        var cells = 0;
        var characters = 0;
        do
        {
            while (reader.Read())
            {
                if (++rows > maxSpreadsheetRows)
                    throw new InvalidDataException("El archivo supera los límites de extracción permitidos.");
                for (var column = 0; column < reader.FieldCount; column++)
                    if (reader.GetValue(column) is { } value)
                    {
                        if (++cells > maxSpreadsheetCells)
                            throw new InvalidDataException("El archivo supera los límites de extracción permitidos.");
                        var text = value.ToString()!;
                        characters += text.Length;
                        if (characters > maxExtractedCharacters)
                            throw new InvalidDataException("El archivo supera los límites de extracción permitidos.");
                        values.Add(text);
                    }
            }
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

}
