using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Azure.Storage.Blobs.Models;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

public class ProjectsService(IBlobService _blobService,
                             IManifestService _manifestService,
                             IUserProvider _userProvider,
                             ITagUtilities _tagUtilities,
                             IWebhookQueueService _webhookQueueService) : IProjectsService
{
    public async Task AssignUserAsync(UserAssignment userAssignment)
    {
        // User assignments are stored as:
        // - 'AssignedBy' on the resource blob's metadata
        // - 'AssignedOn' on the resoure blob's metadata
        // - 'Assignee' on the resource blob's metadata
        // - 'Assignee' on the resource blob's tags

        var targetBlobMetadata = await _blobService.RetrieveBlobMetadataAsync(userAssignment.ResourceFullName, true);

        if (targetBlobMetadata.ContainsKey(Metadata.Assignee))
        {
            var assignee = targetBlobMetadata[Metadata.Assignee];

            // Is the resource assigned to another user?
            if (!assignee.Equals(userAssignment.AssigneeUsername))
            {
                throw new UserAssignmentException(userAssignment.ProjectId,
                                                  userAssignment.ResourceFullName,
                                                  "The specified resource is already assigned to another user.");
            }

            // Is the resource already assigned to the same user?
            if (assignee.Equals(userAssignment.AssigneeUsername))
            {
                return;
            }
        }

        // Set the property/metadata on the resource blob.
        var propertiesToSet = new Dictionary<string, string>
        {
            { Metadata.AssignedBy, userAssignment.AssignedByUsername},
            { Metadata.AssignedOnUtc, userAssignment.AssignedOnUtc.ToString("u")},
            { Metadata.Assignee, userAssignment.AssigneeUsername}
        };

        await _blobService.SetMetadataAsync(userAssignment.ResourceFullName, propertiesToSet);

        // Set the 'Assignee' tag on the resource blob.
        var tagSafeUsername = _tagUtilities.GetSafeTagValue(userAssignment.AssigneeUsername);
        await _blobService.SetTagAsync(userAssignment.ResourceFullName, Tags.Assignee, tagSafeUsername);

        var manifest = await _manifestService.RetrieveManifestAsync(userAssignment.ProjectId) ?? throw new ManifestNotFoundException(userAssignment.ProjectId);

        if (String.IsNullOrWhiteSpace(manifest.WebhookSpecification?.Uri))
        {
            return;
        }

        const string propNameAssignedUsername = "assignedUsername";
        const string propNameResourceFullName = "resourceFullName";

        // Add the username of the assigned user to the custom props.
        var additionalProps = manifest.WebhookSpecification.CustomProps ?? new Dictionary<string, object>();
        additionalProps.Add(propNameAssignedUsername, userAssignment.AssigneeUsername);
        additionalProps.Add(propNameResourceFullName, userAssignment.ResourceFullName);

        await QueueWebhookMessageAsync(manifest.ProjectId,
                                       manifest.PackageName,
                                       manifest.WebhookSpecification.Uri,
                                       additionalProps,
                                       userAssignment.AssignedByUsername,
                                       Events.UserAssigned);
    }

    public async Task<ProjectWithResources> GetAssignedProjectAsync(Guid projectId, string assigneeUsername)
    {
        const int pageSize = 100;

        var blobsWithAttributes = new List<BlobWithAttributes>();
        var pageableResponse = await _blobService.RetrieveByTagAsync(Tags.ProjectId, projectId.ToString(), pageSize);
        blobsWithAttributes.AddRange(pageableResponse.Items);
        var continuationToken = pageableResponse.ContinuationToken;

        while (!String.IsNullOrWhiteSpace(continuationToken))
        {
            pageableResponse = await _blobService.RetrieveByTagAsync(Tags.ProjectId, projectId.ToString(), pageSize, continuationToken);
            blobsWithAttributes.AddRange(pageableResponse.Items);
            continuationToken = pageableResponse.ContinuationToken;
        }

        if (blobsWithAttributes.Count == 0)
        {
            // No project with the specified ID.
            throw new ProjectNotFoundException();
        }

        if (!blobsWithAttributes.Any(blobWithAttribute => blobWithAttribute.Metadata[Metadata.Assignee].Equals(assigneeUsername, StringComparison.OrdinalIgnoreCase)))
        {
            // The specified assignee has not been assigned to this project.
            throw new UnassignedToResourceException();
        }

        var project = new ProjectWithResources
        {
            Id = projectId,
            Name = blobsWithAttributes.First().Metadata[Metadata.ProjectName],
            Username = blobsWithAttributes.First().Metadata[Metadata.CreatedBy]
        };

        var manifestName = _manifestService.GetManifestFullName(projectId);
        var manifestBlob = blobsWithAttributes.SingleOrDefault(blobWithAttributes => blobWithAttributes.Name.Equals(manifestName));

        if (manifestBlob == null)
        {
            // No manifest can be found.
            throw new ManifestNotFoundException(projectId);
        }

        if (!project.State.Equals(ProjectStates.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectStateException();
        }

        project.Resources = blobsWithAttributes.Where(blobWithAttributes => !blobWithAttributes.Name.Equals(manifestName))
                                               .Select(blobWithAttributes => new Resource
                                               {
                                                   AssignedBy = blobWithAttributes.Metadata[Metadata.AssignedBy],
                                                   AssignedOnUtc = DateTime.Parse(blobWithAttributes.Metadata[Metadata.AssignedOnUtc]),
                                                   AssignedTo = blobWithAttributes.Metadata[Metadata.Assignee],
                                                   CreatedBy = blobWithAttributes.Metadata[Metadata.CreatedBy],
                                                   FullName = blobWithAttributes.Name,
                                                   ProjectId = projectId
                                               })
                                               .ToList();

        return project;
    }

    public async Task<IReadOnlyCollection<ProjectSummary>> GetAssignedProjectsAsync(string assigneeUsername)
    {
        const int pageSize = 100;
        var requiredProjectState = ProjectStates.InProgress;
        var tagSafeUsername = _tagUtilities.GetSafeTagValue(assigneeUsername);

        var assignedBlobsWithAttributes = new List<BlobWithAttributes>();
        var pageableResponse = await _blobService.RetrieveByTagAsync(Tags.Assignee, tagSafeUsername, pageSize);
        assignedBlobsWithAttributes.AddRange(pageableResponse.Items);
        var continuationToken = pageableResponse.ContinuationToken;

        while (!String.IsNullOrWhiteSpace(continuationToken))
        {
            pageableResponse = await _blobService.RetrieveByTagAsync(Tags.Assignee, tagSafeUsername, pageSize, continuationToken);
            assignedBlobsWithAttributes.AddRange(pageableResponse.Items);
            continuationToken = pageableResponse.ContinuationToken;
        }

        var tasksAllGetManifestTags = assignedBlobsWithAttributes.Select(blobWithAttributes => blobWithAttributes.Name.Split('/').First())
                                                                 .Distinct()
                                                                 .Select(projectIdAsString =>
                                                                 {
                                                                     var projectId = Guid.Parse(projectIdAsString);
                                                                     var manifestName = _manifestService.GetManifestFullName(projectId);

                                                                     return _blobService.RetrieveTagsAsync(manifestName);
                                                                 })
                                                                 .ToList();

        var allManifestTags = await Task.WhenAll(tasksAllGetManifestTags);

        return allManifestTags.Select(manifestTags => new ProjectSummary
        {
            Id = Guid.Parse(manifestTags[Tags.ProjectId]),
            Name = manifestTags[Tags.ManifestName],
            State = manifestTags[Tags.ProjectState]
        })
        .Where(projectSummary => projectSummary.State.Equals(requiredProjectState))
        .ToList();
    }

    public async Task<ProjectWithResources> GetProjectAsync(Guid projectId)
    {
        var blobItems = await _blobService.RetrieveBlobItemsInFolderAsync(projectId.ToString());

        if (blobItems.Count == 0)
        {
            // Looks like there's no project with the specified ID.
            throw new ProjectNotFoundException(projectId);
        }

        var manifestName = _manifestService.GetManifestFullName(projectId);
        var manifestBlobItem = blobItems.SingleOrDefault(blobItem => blobItem.Name.Equals(manifestName)) ?? throw new ManifestNotFoundException(projectId);

        return new ProjectWithResources
        {
            Id = projectId,
            Name = manifestBlobItem.Tags[Tags.ProjectName],
            Resources = blobItems.DistinctBy(blobItem => blobItem.Name)
                                 .Select(blobItem => new Resource
                                 {
                                     AssignedBy = blobItem.Metadata[Metadata.AssignedBy],
                                     AssignedOnUtc = DateTime.Parse(blobItem.Metadata[Metadata.AssignedOnUtc]),
                                     AssignedTo = blobItem.Metadata[Metadata.Assignee],
                                     CreatedBy = blobItem.Metadata[Metadata.CreatedBy],
                                     FullName = blobItem.Name,
                                     ProjectId = projectId,
                                     ResourceVersions = blobItems.Where(bi => !bi.Name.Equals(manifestName))
                                                                 .Select((blobItem, idx) => new ResourceVersion
                                                                 {
                                                                     FullName = blobItem.Name,
                                                                     ProjectId = projectId,
                                                                     Username = blobItem.Metadata[Metadata.CreatedBy],
                                                                     VersionId = blobItem.VersionId,
                                                                     VersionIndex = idx
                                                                 })
                                                                 .ToList()
                                 })
                                 .ToList(),
            State = manifestBlobItem.Tags[Tags.ProjectState],
            Username = manifestBlobItem.Metadata[Metadata.CreatedBy],
        };
    }

    public async Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId)
    {
        var internalManifest = await GetManifestAsync(projectId);

        return internalManifest?.WebhookSpecification;
    }

    public async Task InitProjectAsync(InternalManifest internalManifest)
    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        if (String.IsNullOrWhiteSpace(internalManifest.PackageName))
        {
            throw new ArgumentNullException(nameof(Project.Name));
        }

        if (String.IsNullOrWhiteSpace(internalManifest.Username))
        {
            throw new ArgumentNullException(nameof(Project.Username));
        }
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

        await _manifestService.StoreManifestAsync(internalManifest);

        if (!String.IsNullOrWhiteSpace(internalManifest.WebhookSpecification?.Uri))
        {
            await QueueWebhookMessageAsync(internalManifest.ProjectId,
                                           internalManifest.PackageName,
                                           internalManifest.WebhookSpecification.Uri,
                                           internalManifest.WebhookSpecification.CustomProps,
                                           internalManifest.Username,
                                           Events.Stored);
        }
    }

    public async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        // Check that no manifest already exists for this project ID.
        return await GetManifestAsync(projectId) == null;
    }

    public async Task RevokeUserAsync(UserAssignment userAssignment)
    {
        // User assignments are stored as:
        // - 'AssignedBy' on the resource blob's metadata
        // - 'AssignedOn' on the resoure blob's metadata
        // - 'Assignee' on the resource blob's metadata
        // - 'Assignee' on the resource blob's tags

        var resourceFullName = userAssignment.ResourceFullName;

        // Remove the properties/metadata on the resource blob.
        await _blobService.RemovePropertiesAsync(resourceFullName, [
                                                                       Metadata.AssignedBy,
                                                                       Metadata.AssignedOnUtc,
                                                                       Metadata.Assignee
                                                                   ]);

        // Remove the 'Assignee' tag on the resource blob.
        await _blobService.RemoveTagAsync(userAssignment.ResourceFullName, Tags.Assignee);

        var manifest = await _manifestService.RetrieveManifestAsync(userAssignment.ProjectId) ?? throw new ManifestNotFoundException(userAssignment.ProjectId);

        if (String.IsNullOrWhiteSpace(manifest.WebhookSpecification?.Uri))
        {
            return;
        }

        const string propNameAssignedUsername = "revokedUsername";
        const string propNameResourceFullName = "resourceFullName";

        // Add the username of the revoked user to the custom props.
        var additionalProps = manifest.WebhookSpecification.CustomProps ?? new Dictionary<string, object>();
        additionalProps.Add(propNameAssignedUsername, userAssignment.AssigneeUsername);
        additionalProps.Add(propNameResourceFullName, userAssignment.ResourceFullName);

        await QueueWebhookMessageAsync(manifest.ProjectId,
                                       manifest.PackageName,
                                       manifest.WebhookSpecification.Uri,
                                       additionalProps,
                                       userAssignment.AssignedByUsername,
                                       Events.UserRevoked);
    }

    public async Task<bool> UpdateProjectStateAsync(Guid projectId, string newState)
    {
        if (!IsProjectStateRecognised(newState))
        {
            throw new InvalidProjectStateException(newState);
        }

        var manifestName = _manifestService.GetManifestFullName(projectId);
        var isTagSet = await _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState);

        if (!isTagSet)
        {
            return false;
        }

        var manifest = await GetManifestAsync(projectId);
        if (manifest?.WebhookSpecification == null)
        {
            return true;
        }
        var stateChangeEventText = GetStateChangeEventText(Events.StateChange, newState);

        await QueueWebhookMessageAsync(projectId,
                                       manifest.PackageName,
                                       manifest.WebhookSpecification.Uri,
                                       manifest.WebhookSpecification.CustomProps,
                                       _userProvider.Username,
                                       stateChangeEventText);

        return true;
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

    private async Task<InternalManifest?> GetManifestAsync(Guid projectId)
    {
        return await _manifestService.RetrieveManifestAsync(projectId);
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
