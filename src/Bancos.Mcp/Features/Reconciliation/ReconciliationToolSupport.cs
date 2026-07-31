using System.Text.Json;
using Bancos.Mcp.Protocol;

namespace Bancos.Mcp.Features.Reconciliation;

internal static class ReconciliationToolSupport
{
    public static bool TryReadGuidArray(JsonElement arguments, string propertyName, out Guid[] ids, out string? error)
    {
        ids = [];
        error = null;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            error = $"Se requiere '{propertyName}' como array de UUID.";
            return false;
        }

        var values = new List<Guid>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var id))
            {
                error = $"Todos los valores de '{propertyName}' deben ser UUID válidos.";
                return false;
            }
            values.Add(id);
        }

        ids = values.ToArray();
        return true;
    }

    public static bool TryReadGuid(JsonElement arguments, string propertyName, out Guid id, out string? error)
    {
        id = default;
        error = null;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String || !Guid.TryParse(element.GetString(), out id))
        {
            error = $"Se requiere '{propertyName}' como UUID válido.";
            return false;
        }
        return true;
    }

    public static bool TryReadRequiredString(JsonElement arguments, string propertyName, out string value, out string? error)
    {
        value = string.Empty;
        error = null;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
        {
            error = $"Se requiere '{propertyName}'.";
            return false;
        }
        value = element.GetString()!;
        return true;
    }

    public static McpToolResult JsonResult(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], value);
    }
}