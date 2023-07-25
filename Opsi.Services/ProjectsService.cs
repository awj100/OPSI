using System.Reflection;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services;

public class ProjectsService : IProjectsService
{
    private readonly ILogger<ProjectsService> _logger;
    private readonly IProjectsTableService _projectsTableService;
    private readonly IResourcesService _resourcesService;
    private readonly IUserProvider _userProvider;
    private readonly IWebhookQueueService _webhookQueueService;

    public ProjectsService(IProjectsTableService projectsTableService,
                           IResourcesService resourcesService,
                           IUserProvider userProvider,
                           IWebhookQueueService QueueService,
                           ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProjectsService>();
        _projectsTableService = projectsTableService;
        _resourcesService = resourcesService;
        _userProvider = userProvider;
        _webhookQueueService = QueueService;
    }

    public async Task<ProjectWithResources?> GetProjectAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            _logger.LogWarning($"{nameof(GetProjectAsync)}: Invalid project ID ({projectId}).");
            return null;
        }

        var resourceEntities = await _resourcesService.GetResourcesAsync(projectId);

        if (!_userProvider.IsAdministrator.Value)
        {
            // Consider only those resources which are assigned to the current user.
            resourceEntities = resourceEntities.Where(resourceEntity => resourceEntity.Username != null && resourceEntity.Username.Equals(_userProvider.Username.Value))
                                               .ToList();
        }

        if (!resourceEntities.Any())
        {
            // If there are no ResourceTableEntity instances then either...
            // - the project ID is invalid (would have already been caught earlier in this method)
            // - this user does not have access to any of the resources, and therefore no access to the project
            _logger.LogWarning($"{nameof(GetProjectAsync)}: No user-accessible resources found for project \"{projectId}\" - user probably doesn't have access.");
            return null;
        }

        var projectWithResources = ProjectWithResources.FromProjectBase(project);

        projectWithResources.Resources = resourceEntities.Select(resourceEntity => resourceEntity.ToResource()).ToList();

        _logger.LogInformation($"{nameof(GetProjectAsync)}: Obtained project with {projectWithResources.Resources.Count} resources.");

        return projectWithResources;
    }

    public async Task<PageableResponse<Project>> GetProjectsAsync(string projectState, int pageSize, string? continuationToken = null)
    {
        if (!IsProjectStateRecognised(projectState))
        {
            throw new ArgumentException("Unrecognised value", nameof(projectState));
        }

        return await _projectsTableService.GetProjectsByStateAsync(projectState, pageSize, continuationToken);
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

    private static bool IsProjectStateRecognised(string projectState)
    {
        foreach (var memberInfo in typeof(ProjectStates).GetMembers(BindingFlags.Public | BindingFlags.Static))
        {
            var memberValue = ((FieldInfo)memberInfo).GetValue(null);

            if (memberValue!.Equals(projectState))
            {
                return true;
            }
        }

        return false;
    }
}
