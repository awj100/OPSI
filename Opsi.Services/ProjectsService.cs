using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services;

public class ProjectsService : IProjectsService
{
    private readonly ICallbackQueueService _callbackQueueService;
    private readonly IProjectsTableService _projectsTableService;

    public ProjectsService(IProjectsTableService projectsTableService, ICallbackQueueService callbackQueueService)
    {
        _callbackQueueService = callbackQueueService;
        _projectsTableService = projectsTableService;
    }

    public async Task<string?> GetCallbackUriAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project?.CallbackUri;
    }

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project == null;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await _projectsTableService.StoreProjectAsync(project);

        await QueueCallbackMessageAsync(project.Id, project.CallbackUri);
    }

    private async Task QueueCallbackMessageAsync(Guid projectId, string callbackUri)
    {
        await _callbackQueueService.QueueCallbackAsync(new InternalCallbackMessage
        {
            ProjectId = projectId,
            RemoteUri = callbackUri,
            Status = "Project created"
        });
    }
}
