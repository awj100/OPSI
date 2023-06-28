﻿using Opsi.AzureStorage.TableEntities;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services;

public class ProjectsService : IProjectsService
{
    private readonly IProjectsTableService _projectsTableService;
    private readonly IUserProvider _userProvider;
    private readonly IWebhookQueueService _webhookQueueService;

    public ProjectsService(IProjectsTableService projectsTableService,
                           IUserProvider userProvider,
                           IWebhookQueueService QueueService)
    {
        _projectsTableService = projectsTableService;
        _userProvider = userProvider;
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

        await QueueWebhookMessageAsync(project.Id, project.WebhookUri);
    }

    private async Task QueueWebhookMessageAsync(Guid projectId, string Uri)
    {
        await _webhookQueueService.QueueWebhookMessageAsync(new InternalWebhookMessage
        {
            ProjectId = projectId,
            RemoteUri = Uri,
            Status = "Project created",
            Username = _userProvider.Username.Value
        });
    }
}
