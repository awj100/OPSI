using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public class ProjectsService : TableServiceBase, IProjectsService
{
    private const string TableName = "projects";
    private readonly ICallbackQueueService _callbackQueueService;

    public ProjectsService(ISettingsProvider settingsProvider, ICallbackQueueService callbackQueueService) : base(settingsProvider, TableName)
    {
        _callbackQueueService = callbackQueueService;
    }

    public async Task<string?> GetCallbackUri(Guid projectId)
    {
        var tableClient = GetTableClient();

        var results = tableClient.QueryAsync<Project>(project => project.Id == projectId);

        await foreach (var result in results)
        {
            return result.CallbackUri;
        }

        return null;
    }

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        var tableClient = GetTableClient();

        var results = tableClient.QueryAsync<Project>(project => project.Id == projectId);

        await foreach (var result in results)
        {
            return false;
        }

        return true;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await StoreTableEntityAsync(project);

        await QueueCallbackMessageAsync(project.Id);
    }

    private async Task QueueCallbackMessageAsync(Guid projectId)
    {
        await _callbackQueueService.QueueCallbackAsync(new CallbackMessage
        {
            ProjectId = projectId,
            Status = "Project created"
        });
    }
}
