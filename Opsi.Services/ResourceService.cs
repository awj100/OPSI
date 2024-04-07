using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

internal class ResourceService(AzureStorage.IResourcesService _resourcesService,
                       AzureStorage.IBlobService _blobService,
                       IWebhookQueueService _webhookQueueService,
                       IProjectsService _projectsService,
                       IUserProvider _userProvider,
                       ILoggerFactory loggerFactory) : IResourceService
{
    private readonly ILogger<ResourceService> _log = loggerFactory.CreateLogger<ResourceService>();

    public async Task<Option<ResourceContent>> GetResourceContentAsync(Guid projectId, string fullName)
    {
        var blobName = $"{projectId}/{fullName}";
        var blobClient = _blobService.RetrieveBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return Option<ResourceContent>.None();
        }

        var blobProps = await blobClient.GetPropertiesAsync();

        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        var contentLength = (int)blobProps.Value.ContentLength;
        var bytes = new byte[contentLength];
        await memoryStream.ReadAsync(bytes.AsMemory(0, contentLength - 1));

        var resourceStorageInfo = new ResourceStorageInfo(projectId, blobClient.Name, memoryStream, _userProvider.Username.Value);
        await QueueWebhookMessageAsync(projectId, resourceStorageInfo, Events.ResourceDownloaded);

        return Option<ResourceContent>.Some(new ResourceContent(blobClient.Name,
                                                                bytes,
                                                                blobProps.Value.ContentLength,
                                                                blobProps.Value.ContentType,
                                                                blobProps.Value.LastModified,
                                                                blobProps.Value.ETag.ToString("H")));
    }

    public async Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername)
    {
        return _userProvider.IsAdministrator.Value
               || await _resourcesService.HasUserAccessAsync(projectId, fullName, requestingUsername);
    }

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        if (!await CanUserStoreFile(resourceStorageInfo))
        {
            throw new ResourceLockConflictException(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value);
        }

        await StoreResourceDataAsync(resourceStorageInfo);

        await QueueWebhookMessageAsync(resourceStorageInfo.ProjectId, resourceStorageInfo, Events.Stored);
    }

    private async Task<bool> CanUserStoreFile(ResourceStorageInfo resourceStorageInfo)
    {
        var blobMetadata = await _blobService.RetrieveBlobMetadataAsync(resourceStorageInfo.FullPath.Value, throwIfNotExists: false);

        // Verify that the uploading user has been assigned to the blob.
        // - i.e., that the uploader is referenced on the resource blob's metadata as 'assignee'.
        if (blobMetadata.TryGetValue(Metadata.Assignee, out var assignee)
            && String.Equals(assignee, resourceStorageInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Verify that the project is still being initialised and that the name of the resource uploader matches the user initialising the project.
        // - The project state is found as a tag on the manifest blob.
        // - The user initialising the project can be found in the manifest.
        var manifestTags = await _blobService.RetrieveTagsAsync(resourceStorageInfo.GetManifestPath(), throwIfNotExists: false);
        var projectState = GetProjectStateFromManifestTags(manifestTags);
        if (!projectState.Equals(ProjectStates.Initialising))
        {
            return false;
        }

        var internalManifest = await RetrieveInternalManifestAsync(resourceStorageInfo);
        return internalManifest != null && internalManifest.Username.Equals(resourceStorageInfo.Username, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId)
    {
        var webhookSpec = await _projectsService.GetWebhookSpecificationAsync(projectId);

        return webhookSpec;
    }

    private async Task<string> StoreResourceDataAsync(ResourceStorageInfo resourceStorageInfo)
    {
        try
        {
            return await _blobService.StoreVersionedResourceAsync(resourceStorageInfo);
        }
        catch (Exception ex)
        {
            const string errorPackage = "An error was encountered while storing a version of the resource.";
            _log.LogError(errorPackage, ex);
            throw new Exception(errorPackage);
        }
    }

    private async Task QueueWebhookMessageAsync(Guid projectId, ResourceStorageInfo resourceStorageInfo, string eventType)
    {
        var webhookSpec = await GetWebhookSpecificationAsync(projectId);
        if (webhookSpec == null)
        {
            return;
        }

        await _webhookQueueService.QueueWebhookMessageAsync(new WebhookMessage
        {
            Event = eventType,
            Level = Levels.Resource,
            Name = resourceStorageInfo.FileName.Value,
            ProjectId = projectId,
            Username = _userProvider.Username.Value
        }, webhookSpec);
    }

    private async Task<InternalManifest?> RetrieveInternalManifestAsync(ResourceStorageInfo resourceStorageInfo)
    {
        using var contentStream = await _blobService.RetrieveContentAsync(resourceStorageInfo.GetManifestPath());

        return await System.Text.Json.JsonSerializer.DeserializeAsync<InternalManifest>(contentStream);
    }

    private static string GetProjectStateFromManifestTags(IDictionary<string, string> tags)
    {
        return tags.TryGetValue(Tags.ProjectState, out string? value) ? value : ProjectStates.Error;
    }
}
