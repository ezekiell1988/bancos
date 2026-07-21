using Bancos.Mcp.Protocol;

namespace Bancos.Mcp.Tools;

public sealed class ToolRegistry(IEnumerable<IMcpTool> tools)
{
    private readonly IReadOnlyDictionary<string, IMcpTool> toolsByName = tools.ToDictionary(tool => tool.Definition.Name, StringComparer.Ordinal);

    public IReadOnlyList<McpToolDefinition> List() => [.. toolsByName.Values.Select(tool => tool.Definition)];

    public bool TryGet(string name, out IMcpTool? tool) => toolsByName.TryGetValue(name, out tool);
}
