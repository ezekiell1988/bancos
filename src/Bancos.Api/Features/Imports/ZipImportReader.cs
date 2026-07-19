using System.IO.Compression;

namespace Bancos.Api.Features.Imports;

internal sealed record ImportSource(int EntryIndex, string Path, byte[] Content);

internal static class ZipImportReader
{
    private static readonly string[] AllowedExtensions = [".csv", ".xlsx", ".xls", ".pdf"];
    private const int MaxEntries = 100;
    private const long MaxEntryBytes = 20 * 1024 * 1024;
    private const long MaxTotalBytes = 50 * 1024 * 1024;

    public static IReadOnlyList<ImportSource> Read(string fileName, byte[] content)
    {
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return [new(0, Path.GetFileName(fileName), content)];
        using var archive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read, leaveOpen: false);
        var files = archive.Entries.Where(entry =>
            !string.IsNullOrEmpty(entry.Name)
            && !entry.Name.StartsWith("._", StringComparison.Ordinal)
            && !entry.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)
            && AllowedExtensions.Contains(Path.GetExtension(entry.Name), StringComparer.OrdinalIgnoreCase)).ToArray();
        if (files.Length > MaxEntries) throw new InvalidDataException("El ZIP excede el máximo de 100 archivos.");

        long total = 0;
        var sources = new List<ImportSource>(files.Length);
        for (var entryIndex = 0; entryIndex < files.Length; entryIndex++)
        {
            var entry = files[entryIndex];
            if (entry.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(entry.FullName)) throw new InvalidDataException("El ZIP contiene una ruta no permitida.");
            if (entry.Length > MaxEntryBytes) throw new InvalidDataException("Una entrada del ZIP excede 20 MB.");
            total += entry.Length;
            if (total > MaxTotalBytes) throw new InvalidDataException("El ZIP excede 50 MB descomprimidos.");
            using var stream = entry.Open(); using var buffer = new MemoryStream(); stream.CopyTo(buffer);
            sources.Add(new(entryIndex, entry.FullName.Replace('\\', '/'), buffer.ToArray()));
        }
        return sources;
    }
}
