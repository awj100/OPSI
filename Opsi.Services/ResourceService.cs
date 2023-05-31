using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Common.Exceptions;
using Opsi.Pocos;

namespace Opsi.Services;

internal class ResourceService : IResourceService
{
    private readonly AzureStorage.IBlobService _blobService;
    private readonly ICallbackQueueService _callbackQueueService;
    private readonly ILogger<ResourceService> _log;
    private readonly AzureStorage.IResourcesService _resourcesService;

    public ResourceService(AzureStorage.IResourcesService resourcesService,
                           AzureStorage.IBlobService blobService,
                           ICallbackQueueService callbackQueueService,
                           ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _callbackQueueService = callbackQueueService;
        _log = loggerFactory.CreateLogger<ResourceService>();
        _resourcesService = resourcesService;
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

        await QueueCallbackMessageAsync(resourceStorageInfo.ProjectId);

        if (currentVersionInfo.LockedTo.IsSome)
        {
            await UnlockFileAsync(resourceStorageInfo);
        }
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

    private async Task UnlockFileAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await _resourcesService.UnlockResourceFromUser(resourceStorageInfo.ProjectId, resourceStorageInfo.FullPath.Value, resourceStorageInfo.Username);
    }

    private static bool CanUserStoreFile(VersionInfo versionInfo, string username)
    {
        return versionInfo.LockedTo.IsNone || versionInfo.LockedTo.Value == username;
    }

    private async Task QueueCallbackMessageAsync(Guid projectId)
    {
        await _callbackQueueService.QueueCallbackAsync(new CallbackMessage
        {
            ProjectId = projectId,
            Status = "Resource stored"
        });
    }
}
