using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var endpoint = new Uri(args.FirstOrDefault() ?? "http://127.0.0.1:3001/mcp");

var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = endpoint,
    TransportMode = HttpTransportMode.StreamableHttp,
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

Console.WriteLine("SMOKE TEST: TODO OK");

static string FirstText(CallToolResult result) =>
    result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "<no text content>";

static void Check(string name, bool condition, string detail)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL {name} — {detail}");

    Console.WriteLine($"OK   {name}");
}

