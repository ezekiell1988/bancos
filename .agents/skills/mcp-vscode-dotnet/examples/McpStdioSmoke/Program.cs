using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var serverProject = Path.GetFullPath(
    args.FirstOrDefault() ?? "examples/McpStdioServer/McpStdioServer.csproj");

if (!File.Exists(serverProject))
    throw new FileNotFoundException("MCP stdio server project not found.", serverProject);

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "company-mcp-stdio-smoke",
    Command = "dotnet",
    Arguments =
    [
        "run",
        "--project",
        serverProject,
        "--no-launch-profile",
        "--tl:off",
        "--verbosity",
        "quiet",
    ],
    WorkingDirectory = Path.GetDirectoryName(serverProject),
});

await using var client = await McpClient.CreateAsync(transport);

var tools = await client.ListToolsAsync();
var names = tools.Select(tool => tool.Name).Order().ToArray();

Check("catalog contains server_time", names.Contains("server_time"), string.Join(", ", names));
Check("catalog contains tickets_create", names.Contains("tickets_create"), string.Join(", ", names));

var timeResult = await client.CallToolAsync(
    "server_time",
    new Dictionary<string, object?>(),
    cancellationToken: CancellationToken.None);

Check("server_time succeeds", timeResult.IsError is not true, FirstText(timeResult));

var previewResult = await client.CallToolAsync(
    "tickets_create",
    new Dictionary<string, object?>
    {
        ["title"] = "[MCP TEST] Preview ticket",
        ["apply"] = false,
    },
    cancellationToken: CancellationToken.None);

var previewText = FirstText(previewResult);
Check("tickets_create preview succeeds", previewResult.IsError is not true, previewText);
Check("preview does not apply", previewText.Contains("\"applied\":false", StringComparison.OrdinalIgnoreCase), previewText);

Console.Error.WriteLine("STDIO SMOKE TEST: TODO OK");

static string FirstText(CallToolResult result) =>
    result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "<no text content>";

static void Check(string name, bool condition, string detail)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL {name} — {detail}");

    Console.Error.WriteLine($"OK   {name}");
}
