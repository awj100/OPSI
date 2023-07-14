using Opsi.Constants.Webhooks;
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

    public async Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        if (project == null || String.IsNullOrWhiteSpace(project.WebhookSpecification?.Uri))
        {
            return null;
        }

        return project.WebhookSpecification;
    }

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project == null;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await _projectsTableService.StoreProjectAsync(project);

        if (!String.IsNullOrWhiteSpace(project.WebhookSpecification?.Uri))
        {
            await QueueWebhookMessageAsync(project.Id,
                                           project.Name,
                                           project.WebhookSpecification.Uri,
                                           project.WebhookSpecification.CustomProps,
                                           project.Username);
        }
    }

    private async Task QueueWebhookMessageAsync(Guid projectId,
                                                string projectName,
                                                string? webhookRemoteUri,
                                                Dictionary<string, object> webhookCustomProps,
                                                string username)
    {
        await _webhookQueueService.QueueWebhookMessageAsync(new WebhookMessage
        {
            Event = Events.Stored,
            Level = Levels.Project,
            Name = projectName,
            ProjectId = projectId,
            Username = username
        }, new ConsumerWebhookSpecification
        {
            CustomProps = webhookCustomProps,
            Uri = webhookRemoteUri
        });
    }
}
