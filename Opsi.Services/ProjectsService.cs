using System.Reflection;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services;

public class ProjectsService(IProjectsTableService _projectsTableService,
                             IBlobService _blobService,
                             ITagUtilities _tagUtilities,
                             IWebhookQueueService _webhookQueueService) : IProjectsService
{
    public async Task AssignUserAsync(UserAssignment userAssignment)
    {
        var project = await GetProjectAsync(userAssignment.ProjectId);

        var assingmentsOnRequestedResource = project.Resources
                                                    .Where(resource => resource.FullName.Equals(userAssignment.ResourceFullName, StringComparison.OrdinalIgnoreCase) && resource.AssignedTo != null)
                                                    .ToList();

        // Is the resource assigned to another user?
        if (assingmentsOnRequestedResource.Any(resource => !resource.AssignedTo!.Equals(userAssignment.AssigneeUsername, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UserAssignmentException(userAssignment.ProjectId,
                                              userAssignment.ResourceFullName,
                                              "The specified resource is already assigned to another user.");
        }

        // Is the resource already assigned to the same user?
        if (assingmentsOnRequestedResource.Any(resource => resource.AssignedTo!.Equals(userAssignment.AssigneeUsername, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        userAssignment.ProjectName = project.Name;

        var webhookSpecification = await GetWebhookSpecificationAsync(userAssignment.ProjectId);

        if (webhookSpecification == null)
        {
            return;
        }

        await _projectsTableService.AssignUserAsync(userAssignment);

        if (!String.IsNullOrWhiteSpace(webhookSpecification?.Uri))
        {
            const string propNameAssignedUsername = "assignedUsername";
            const string propNameResourceFullName = "resourceFullName";

            // Add the username of the assigned user to the custom props.
            var additionalProps = webhookSpecification.CustomProps ?? new Dictionary<string, object>();
            additionalProps.Add(propNameAssignedUsername, userAssignment.AssigneeUsername);
            additionalProps.Add(propNameResourceFullName, userAssignment.ResourceFullName);

            await QueueWebhookMessageAsync(project.Id,
                                           project.Name,
                                           webhookSpecification.Uri,
                                           additionalProps,
                                           userAssignment.AssignedByUsername,
                                           Events.UserAssigned);
        }
    }

    public async Task<ProjectWithResources> GetAssignedProjectAsync(Guid projectId, string assigneeUsername)
    {
        var tableEntities = await _projectsTableService.GetProjectEntitiesAsync(projectId, assigneeUsername);

        if (!tableEntities.Any())
        {
            // No project with the specified ID.
            throw new ProjectNotFoundException();
        }

        if (!tableEntities.Any(entity => entity.GetType() == typeof(UserAssignmentTableEntity)))
        {
            // The specified assignee has not been assigned to this project.
            throw new UnassignedToProjectException();
        }

        if (tableEntities.SingleOrDefault(entity => entity.GetType() == typeof(ProjectTableEntity)) is not ProjectTableEntity projectTableEntity)
        {
            throw new ProjectNotFoundException();
        }

        var project = projectTableEntity.ToProject();
        var projectWithResources = ProjectWithResources.FromProjectBase(project);

        if (!project.State.Equals(ProjectStates.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectStateException();
        }

        projectWithResources.Resources = tableEntities.Where(entity => entity.GetType() == typeof(ResourceTableEntity))
                                                      .Cast<ResourceTableEntity>()
                                                      .Select(resourceTableEntity => resourceTableEntity.ToResource())
                                                      .DistinctBy(resource => resource.FullName)
                                                      .ToList();

        return projectWithResources;
    }

    public async Task<IReadOnlyCollection<UserAssignment>> GetAssignedProjectsAsync(string assigneeUsername)
    {
        var userAssignmentTableEntities = await _projectsTableService.GetAssignedProjectsAsync(assigneeUsername);

        return userAssignmentTableEntities.Select(te => te.ToUserAssignment()).ToList();
    }

    public async Task<ProjectWithResources> GetProjectAsync(Guid projectId)
    {
        var tableEntities = await _projectsTableService.GetProjectEntitiesAsync(projectId);

        if (!tableEntities.Any())
        {
            // No project with the specified ID.
            throw new ProjectNotFoundException();
        }

        if (tableEntities.SingleOrDefault(entity => entity.GetType() == typeof(ProjectTableEntity)) is not ProjectTableEntity projectTableEntity)
        {
            throw new ProjectNotFoundException();
        }

        var project = projectTableEntity.ToProject();
        var projectWithResources = ProjectWithResources.FromProjectBase(project);

        var userAssignments = tableEntities.OfType<UserAssignmentTableEntity>()
                                           .Select(userAssignment => userAssignment.ToUserAssignment())
                                           .ToList();

        var resourceVersions = tableEntities.OfType<ResourceVersionTableEntity>()
                                            .Select(resourceVersion => resourceVersion.ToResourceVersion())
                                            .ToList() ?? new List<ResourceVersion>(0);

        projectWithResources.Resources = tableEntities.OfType<ResourceTableEntity>()
                                                      .Select(GetAssignmentPopulatedResource)
                                                      .Select(GetVersionPopulatedResource)
                                                      .ToList();

        return projectWithResources;

        Resource GetAssignmentPopulatedResource(Resource resource)
        {
            var userAssignment = userAssignments?.SingleOrDefault(ua => ua.ResourceFullName.Equals(resource.FullName, StringComparison.OrdinalIgnoreCase));
            if (userAssignment != null)
            {
                resource.AssignedBy = userAssignment.AssignedByUsername;
                resource.AssignedOnUtc = userAssignment.AssignedOnUtc;
                resource.AssignedTo = userAssignment.AssigneeUsername;
            }

            return resource;
        }

        Resource GetVersionPopulatedResource(Resource resource)
        {
            resource.ResourceVersions.AddRange(resourceVersions!.Where(resourceVersion => resourceVersion.FullName.Equals(resource.FullName, StringComparison.OrdinalIgnoreCase)));

            return resource;
        }
    }

    public async Task<PageableResponse<OrderedProject>> GetProjectsAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null)
    {
        if (!IsProjectStateRecognised(projectState))
        {
            throw new ArgumentException("Unrecognised value", nameof(projectState));
        }

        return await _projectsTableService.GetProjectsByStateAsync(projectState, orderBy, pageSize, continuationToken);
    }

    public async Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        if (project.IsNone || String.IsNullOrWhiteSpace(project.Value.WebhookSpecification?.Uri))
        {
            return null;
        }

        return project.Value.WebhookSpecification;
    }

    public async Task InitProjectAsync(Project project)
    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        if (String.IsNullOrWhiteSpace(project.Name))
        {
            throw new ArgumentNullException(nameof(Project.Name));
        }

        if (String.IsNullOrWhiteSpace(project.Username))
        {
            throw new ArgumentNullException(nameof(Project.Username));
        }
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

        var fullName = $"{project.Id}/{Tags.TagsHostName}";
        var initialState = ProjectStates.Initialising;
        var stream = new MemoryStream(1);
        stream.WriteByte(0);
        stream.Position = 0;

        try
        {
            await _blobService.StoreAsync(fullName, stream);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to create project with ID \"{project.Id}\": {exception.Message}");
        }

        var metadata = new Dictionary<string, string>
        {
            {Metadata.CreatedBy, project.Username},
            {Metadata.ProjectId, project.Id.ToString()},
            {Metadata.ProjectName, project.Name}
        };

        try
        {
            await _blobService.SetMetadataAsync(fullName, metadata);
        }
        catch(Exception exception)
        {
            try
            {
                await _blobService.DeleteAsync(fullName);
            }
            catch(Exception)
            {}

            throw new Exception($"Failed to set initial metadata on project with ID \"{project.Id}\": {exception.Message}");
        }

        var tags = new Dictionary<string, string>
        {
            {Tags.ProjectId, _tagUtilities.GetSafeTagValue(project.Id)},
            {Tags.ProjectName, _tagUtilities.GetSafeTagValue(project.Name)},
            {Tags.ProjectState, _tagUtilities.GetSafeTagValue(initialState)}
        };

        try
        {
            await _blobService.SetTagsAsync(fullName, tags);
        }
        catch(Exception exception)
        {
            try
            {
                await _blobService.DeleteAsync(fullName);
            }
            catch(Exception)
            {}

            throw new Exception($"Failed to set initial tags on project with ID \"{project.Id}\": {exception.Message}");
        }

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

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        var project = await _projectsTableService.GetProjectByIdAsync(projectId);

        return project.IsNone;
    }

    public async Task RevokeUserAsync(UserAssignment userAssignment)
    {
        var optProject = await _projectsTableService.GetProjectByIdAsync(userAssignment.ProjectId);
        if (optProject.IsNone)
        {
            throw new ArgumentException("Invalid project ID");
        }
        var project = optProject.Value;

        userAssignment.ProjectName = optProject.Value.Name;

        await _projectsTableService.RevokeUserAsync(userAssignment);

        if (!String.IsNullOrWhiteSpace(project.WebhookSpecification?.Uri))
        {
            const string propNameAssignedUsername = "revokedUsername";
            const string propNameResourceFullName = "resourceFullName";

            // Add the username of the revoked user to the custom props.
            var additionalProps = project.WebhookSpecification.CustomProps ?? new Dictionary<string, object>();
            additionalProps.Add(propNameAssignedUsername, userAssignment.AssigneeUsername);
            additionalProps.Add(propNameResourceFullName, userAssignment.ResourceFullName);

            await QueueWebhookMessageAsync(project.Id,
                                           project.Name,
                                           project.WebhookSpecification.Uri,
                                           additionalProps,
                                           userAssignment.AssignedByUsername,
                                           Events.UserRevoked);
        }
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
        var updatedProjectTableEntity = await _projectsTableService.UpdateProjectStateAsync(projectId, newState);
        if (updatedProjectTableEntity.IsNone)
        {
            // Nothing was updated.
            return;
        }

        var project = updatedProjectTableEntity.Value.ToProject();
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
