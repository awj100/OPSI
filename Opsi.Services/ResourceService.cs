using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

internal class ResourceService : IResourceService
{
    private readonly AzureStorage.IBlobService _blobService;
    private readonly ILogger<ResourceService> _log;
    private readonly IProjectsService _projectsService;
    private readonly AzureStorage.IResourcesService _resourcesService;
    private readonly IUserProvider _userProvider;
    private readonly IWebhookQueueService _webhookQueueService;

    public ResourceService(AzureStorage.IResourcesService resourcesService,
                           AzureStorage.IBlobService blobService,
                           IWebhookQueueService webhookQueueService,
                           IProjectsService projectsService,
                           IUserProvider userProvider,
                           ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _projectsService = projectsService;
        _log = loggerFactory.CreateLogger<ResourceService>();
        _resourcesService = resourcesService;
        _userProvider = userProvider;
        _webhookQueueService = webhookQueueService;
    }

    public async Task<Option<ResourceContent>> GetResourceContentAsync(Guid projectId, string fullName)
    {
        var blobName = $"{projectId}/{fullName}";
        var blobClient = _blobService.RetrieveBlob(blobName);

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
        await memoryStream.ReadAsync(bytes, 0, contentLength - 1);

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
        var currentVersionInfo = await GetVersionInfoAsync(resourceStorageInfo);

        if (!CanUserStoreFile(currentVersionInfo, resourceStorageInfo.Username))
        {
            throw new ResourceLockConflictException(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value);
        }

        var versionedResourceStorageInfo = resourceStorageInfo.ToVersionedResourceStorageInfo(currentVersionInfo.GetNextVersionInfo());

        await StoreFileDataAndVersionAsync(versionedResourceStorageInfo);

        await QueueWebhookMessageAsync(resourceStorageInfo.ProjectId, resourceStorageInfo, Events.Stored);
    }

    private async Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId)
    {
        var webhookSpec = await _projectsService.GetWebhookSpecificationAsync(projectId);

        return webhookSpec;
    }

    private async Task<VersionInfo> GetVersionInfoAsync(ResourceStorageInfo resourceStorageInfo)
    {
        try
        {
            return await _resourcesService.GetCurrentVersionInfo(resourceStorageInfo.ProjectId, resourceStorageInfo.RestOfPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to obtain version information for {nameof(resourceStorageInfo.ProjectId)} = \"{resourceStorageInfo.ProjectId}\", {nameof(resourceStorageInfo.FullPath)} = \"{resourceStorageInfo.FullPath.Value}\".", ex);
        }
    }

    private async Task StoreFileDataAndVersionAsync(VersionedResourceStorageInfo versionedResourceStorageInfo)
    {
        try
        {
            // Store the blob.
            versionedResourceStorageInfo.VersionId = await _blobService.StoreVersionedFileAsync(versionedResourceStorageInfo);
        }
        catch (Exception ex)
        {
            const string errorPackage = "An error was encountered while storing the file data.";
            _log.LogError(errorPackage, ex);
            throw new Exception(errorPackage);
        }

        try
        {
            // Record the resource upload in the 'resources' table.
            await _resourcesService.StoreResourceAsync(versionedResourceStorageInfo);
        }
        catch (Exception ex)
        {
            try
            {
                // Remove the blob before throwing the exception.
                await _blobService.DeleteAsync(versionedResourceStorageInfo.FullPath.Value);
            }
            catch (Exception)
            {
            }

            const string errorMessage = "An error was encountered while recording the resource.";
            _log.LogError(errorMessage, ex);
            throw new Exception(errorMessage);
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

    private static bool CanUserStoreFile(VersionInfo versionInfo, string username)
    {
        return versionInfo.AssignedTo.IsNone || versionInfo.AssignedTo.Value == username;
    }
}
