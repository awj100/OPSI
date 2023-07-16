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
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        if (String.IsNullOrWhiteSpace(project.Name))
        {
            throw new ArgumentNullException(nameof(Project.Name));
        }

        if (String.IsNullOrWhiteSpace(project.State))
        {
            throw new ArgumentNullException(nameof(Project.State));
        }

        if (String.IsNullOrWhiteSpace(project.Username))
        {
            throw new ArgumentNullException(nameof(Project.Username));
        }
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

        await _projectsTableService.StoreProjectAsync(project);

        if (!String.IsNullOrWhiteSpace(project.WebhookSpecification?.Uri))
        {
            await QueueWebhookMessageAsync(project.Id,
                                           project.Name,
                                           project.WebhookSpecification.Uri,
                                           project.WebhookSpecification.CustomProps,
                                           project.Username,
                                           Events.Stored);
        }
    }

    public async Task UpdateProjectStateAsync(Guid projectId, string newState)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        if (project == null)
        {
            return;
        }

        if (project != null && project.State!.Equals(newState))
        {
            return;
        }

        project!.State = newState;

        await _projectsTableService.UpdateProjectAsync(project);

        var stateChangeEventText = GetStateChangeEventText(Events.StateChange, newState);
        await QueueWebhookMessageAsync(project.Id,
                                       project.Name!,
                                       project.WebhookSpecification?.Uri,
                                       project.WebhookSpecification?.CustomProps,
                                       project.Username!,
                                       stateChangeEventText);
    }

    private async Task QueueWebhookMessageAsync(Guid projectId,
                                                string projectName,
                                                string? webhookRemoteUri,
                                                Dictionary<string, object>? webhookCustomProps,
                                                string username,
                                                string eventText)
    {
        await _webhookQueueService.QueueWebhookMessageAsync(new WebhookMessage
        {
            Event = eventText,
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

    private static string GetStateChangeEventText(string eventText, string newState)
    {
        return $"{eventText}:{newState}";
    }
}
