using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services;

public class ProjectsService : IProjectsService
{
    private readonly IProjectsTableService _projectsTableService;
    private readonly IWebhookQueueService _webhookQueueService;

    public ProjectsService(IProjectsTableService projectsTableService, IWebhookQueueService QueueService)
    {
        _projectsTableService = projectsTableService;
        _webhookQueueService = QueueService;
    }

    public async Task<string?> GetWebhookUriAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project?.WebhookUri;
    }

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project == null;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await _projectsTableService.StoreProjectAsync(project);

        await QueueWebhookMessageAsync(project.Id, project.WebhookUri, project.Username);
    }

    private async Task QueueWebhookMessageAsync(Guid projectId, string uri, string username)
    {
        await _webhookQueueService.QueueWebhookMessageAsync(new WebhookMessage
        {
            ProjectId = projectId,
            Status = "Project created",
            Username = username
        }, uri);
    }
}
