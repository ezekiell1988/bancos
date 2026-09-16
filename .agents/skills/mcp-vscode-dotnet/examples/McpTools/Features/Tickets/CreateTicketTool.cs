using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CompanyMcp.Tools.Features.Tickets;

[McpServerToolType]
public static class CreateTicketTool
{
    [McpServerTool(
        Name = "tickets_create",
        Title = "Create ticket",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = true,
        UseStructuredContent = true)]
    [Description(
        "Previews or creates a ticket. Calls without apply=true never mutate state. " +
        "Use it only after the user has provided a clear ticket title.")]
    public static async Task<TicketChangeResult> CreateAsync(
        [Description("Ticket title, between 3 and 120 characters.")] string title,
        ITicketService tickets,
        [Description("Set true to create the ticket; false returns preview only.")] bool apply = false,
        CancellationToken ct = default)
    {
        title = title.Trim();
        if (title.Length is < 3 or > 120)
            throw new McpException("title must contain between 3 and 120 characters");

        if (!apply)
        {
            return new(
                Preview: true,
                Applied: false,
                Message: "Preview only. Call again with apply=true to create the ticket.",
                Title: title,
                TicketId: null);
        }

        var ticketId = await tickets.CreateAsync(title, ct);
        return new(
            Preview: false,
            Applied: true,
            Message: "Ticket created.",
            Title: title,
            TicketId: ticketId);
    }
}

public sealed record TicketChangeResult(
    bool Preview,
    bool Applied,
    string Message,
    string Title,
    long? TicketId);

public interface ITicketService
{
    Task<long> CreateAsync(string title, CancellationToken ct);
}

// Demo only. Replace with a durable, externally backed implementation before scale-out.
public sealed class DemoTicketService : ITicketService
{
    private long _lastId = 1000;

    public Task<long> CreateAsync(string title, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Interlocked.Increment(ref _lastId));
    }
}
