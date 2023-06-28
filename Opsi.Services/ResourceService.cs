using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Common.Exceptions;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

internal class ResourceService : IResourceService
{
    private readonly AzureStorage.IBlobService _blobService;
    private readonly ILogger<ResourceService> _log;
    private readonly IProjectsService _projectsService;
    private readonly AzureStorage.IResourcesService _resourcesService;
    private readonly IWebhookQueueService _webhookQueueService;

    public ResourceService(AzureStorage.IResourcesService resourcesService,
                           AzureStorage.IBlobService blobService,
                           IWebhookQueueService webhookQueueService,
                           IProjectsService projectsService,
                           ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _projectsService = projectsService;
        _log = loggerFactory.CreateLogger<ResourceService>();
        _resourcesService = resourcesService;
        _webhookQueueService = webhookQueueService;
    }

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var currentVersionInfo = await GetVersionInfoAsync(resourceStorageInfo);

        if (!CanUserStoreFile(currentVersionInfo, resourceStorageInfo.Username))
        {
            throw new ResourceLockConflictException(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value);
        }

        resourceStorageInfo.VersionInfo = currentVersionInfo.GetNextVersionInfo();

        await StoreFileDataAndVersionAsync(resourceStorageInfo);

        await QueueWebhookMessageAsync(resourceStorageInfo.ProjectId, resourceStorageInfo);

        if (currentVersionInfo.LockedTo.IsSome)
        {
            await UnlockFileAsync(resourceStorageInfo);
        }
    }

    private async Task<string?> GetWebhookRemoteUriAsync(Guid projectId)
    {
        return await _projectsService.GetWebhookUriAsync(projectId);
    }

    private async Task<VersionInfo> GetVersionInfoAsync(ResourceStorageInfo resourceStorageInfo)
    {
        try
        {
            return await _resourcesService.GetCurrentVersionInfo(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to obtain version information for {nameof(resourceStorageInfo.ProjectId)} = \"{resourceStorageInfo.ProjectId}\", {nameof(resourceStorageInfo.FullPath)} = \"{resourceStorageInfo.FullPath.Value}\".", ex);
        }
    }

    private async Task StoreFileDataAndVersionAsync(ResourceStorageInfo resourceStorageInfo)
    {
        try
        {
            // Store the blob.
            resourceStorageInfo.VersionId = await _blobService.StoreVersionedFileAsync(resourceStorageInfo);
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
            await _resourcesService.StoreResourceAsync(resourceStorageInfo);
        }
        catch (Exception ex)
        {
            try
            {
                // Remove the blob before throwing the exception.
                await _blobService.DeleteAsync(resourceStorageInfo.FullPath.Value);
            }
            catch (Exception)
            {
            }

            const string errorMessage = "An error was encountered while recording the resource.";
            _log.LogError(errorMessage, ex);
            throw new Exception(errorMessage);
        }
    }

    private async Task QueueWebhookMessageAsync(Guid projectId, ResourceStorageInfo resourceStorageInfo)
    {
        var remoteUri = await GetWebhookRemoteUriAsync(projectId);
        if (remoteUri == null)
        {
            return;
        }

        await _webhookQueueService.QueueWebhookMessageAsync(new InternalWebhookMessage
        {
            ProjectId = projectId,
            RemoteUri = remoteUri,
            Status = $"Resource stored: {resourceStorageInfo.FileName}"
        });
    }

    private async Task UnlockFileAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await _resourcesService.UnlockResourceFromUser(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value, resourceStorageInfo.Username);
    }

    private static bool CanUserStoreFile(VersionInfo versionInfo, string username)
    {
        return versionInfo.LockedTo.IsNone || versionInfo.LockedTo.Value == username;
    }
}
