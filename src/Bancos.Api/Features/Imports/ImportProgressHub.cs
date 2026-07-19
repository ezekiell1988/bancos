using Microsoft.AspNetCore.SignalR;

namespace Bancos.Api.Features.Imports;

public interface IImportProgressClient
{
    Task ProgressUpdated(ImportProgressResponse progress);
}

public sealed class ImportProgressHub : Hub<IImportProgressClient>
{
    public Task Subscribe(Guid importId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(importId));
    public Task Unsubscribe(Guid importId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(importId));

    internal static string GroupName(Guid importId) => $"import-progress:{importId:N}";
}

public interface IImportProgressPublisher
{
    Task PublishAsync(ImportProgressResponse progress, CancellationToken cancellationToken);
}

public sealed class SignalRImportProgressPublisher(IHubContext<ImportProgressHub, IImportProgressClient> hub) : IImportProgressPublisher
{
    public Task PublishAsync(ImportProgressResponse progress, CancellationToken cancellationToken) =>
        hub.Clients.Group(ImportProgressHub.GroupName(progress.ImportId)).ProgressUpdated(progress);
}
