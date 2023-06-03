using Azure.Data.Tables;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;

namespace Opsi.AzureStorage;

internal class ResourcesService : IResourcesService
{
    private const int StringComparisonMatch = 0;
    private const string TableName = "resources";
    private readonly ITableService _tableService;

    public ResourcesService(ISettingsProvider settingsProvider, ITableServiceFactory tableServiceFactory)
    {
        _tableService = tableServiceFactory.Create(TableName);
    }

    public async Task DeleteResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await DeleteResourceAsync(resourceStorageInfo.ProjectId, resourceStorageInfo.RestOfPath!);
    }

    public async Task DeleteResourceAsync(Resource resource)
    {
        await DeleteResourceAsync(resource.ProjectId, resource.FullName!);
    }

    public async Task DeleteResourceAsync(Guid projectId, string fullName)
    {
        await _tableService.DeleteTableEntityAsync(projectId.ToString(), fullName);
    }

    public async Task<VersionInfo> GetCurrentVersionInfo(Guid projectId, string fullName)
    {
        var key = projectId.ToString();
        var resources = new List<Resource>();
        var tableClient = _tableService.GetTableClient();

        var pageableResources = tableClient.QueryAsync<Resource>(resource => resource.PartitionKey == projectId.ToString()
                                                                             && String.Compare(resource.FullName, fullName, StringComparison.OrdinalIgnoreCase) == StringComparisonMatch);

        await foreach (var resource in pageableResources)
        {
            resources.Add(resource);
        }

        var latestVersion = resources.OrderByDescending(resource => resource.VersionIndex).FirstOrDefault();

        return latestVersion != null
            ? new VersionInfo(latestVersion.VersionIndex, latestVersion.LockedTo)
            : new VersionInfo(0);
    }

    public async Task LockResourceToUser(Guid projectId, string fullName, string username)
    {
        var tableClient = _tableService.GetTableClient();

        var latestResource = await GetResourceForLockOrUnlockAsync(tableClient, projectId, fullName);

        if (!String.IsNullOrWhiteSpace(latestResource.Username) && !String.Equals(latestResource.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            throw new ResourceLockConflictException(projectId, fullName);
        }

        latestResource.LockedTo = username;

        var response = await tableClient.UpdateEntityAsync(latestResource, Azure.ETag.All);

        if (response.Status != 204)
        {
            throw new ResourceLockException(projectId, fullName, response.ReasonPhrase);
        }
    }

    public async Task<IReadOnlyCollection<Resource>> GetResourcesAsync(Guid projectId)
    {
        var key = projectId.ToString();
        var resources = new List<Resource>();

        var tableClient = _tableService.GetTableClient();
        var pageableResources = tableClient.QueryAsync<Resource>(x => x.PartitionKey == projectId.ToString());

        await foreach (var resource in pageableResources)
        {
            resources.Add(resource);
        }

        return resources;
    }

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var resource = new Resource
        {
            FullName = resourceStorageInfo.RestOfPath,
            LockedTo = resourceStorageInfo.VersionInfo.LockedTo.IsSome ? resourceStorageInfo.VersionInfo.LockedTo.Value : null,
            ProjectId = resourceStorageInfo.ProjectId,
            Username = resourceStorageInfo.Username,
            VersionId = resourceStorageInfo.VersionId,
            VersionIndex = resourceStorageInfo.VersionInfo.Index
        };

        await _tableService.StoreTableEntityAsync(resource);
    }

    public async Task UnlockResource(Guid projectId, string fullName)
    {
        var tableClient = _tableService.GetTableClient();

        var latestResource = await GetResourceForLockOrUnlockAsync(tableClient, projectId, fullName);

        if (String.IsNullOrWhiteSpace(latestResource.Username))
        {
            return;
        }

        latestResource.LockedTo = null;

        var response = await tableClient.UpdateEntityAsync(latestResource, Azure.ETag.All);

        if (response.Status != 204)
        {
            throw new ResourceLockException(projectId, fullName, response.ReasonPhrase);
        }
    }

    public async Task UnlockResourceFromUser(Guid projectId, string fullName, string username)
    {
        var tableClient = _tableService.GetTableClient();

        var latestResource = await GetResourceForLockOrUnlockAsync(tableClient, projectId, fullName);

        if (String.IsNullOrWhiteSpace(latestResource.Username))
        {
            return;
        }

        if (!String.Equals(latestResource.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            throw new ResourceLockConflictException(projectId, fullName);
        }

        latestResource.LockedTo = null;

        var response = await tableClient.UpdateEntityAsync(latestResource, Azure.ETag.All);

        if (response.Status != 204)
        {
            throw new ResourceLockException(projectId, fullName, response.ReasonPhrase);
        }
    }

    private static async Task<Resource> GetResourceForLockOrUnlockAsync(TableClient tableClient, Guid projectId, string fullName)
    {
        var allResources = new List<Resource>();

        var key = projectId.ToString();
        var resources = new List<Resource>();
        var matchingResources = tableClient.QueryAsync<Resource>(resource => resource.PartitionKey == projectId.ToString()
                                                                            && String.Compare(resource.FullName, fullName, StringComparison.OrdinalIgnoreCase) == StringComparisonMatch);

        await foreach (var resource in matchingResources)
        {
            allResources.Add(resource);
        }

        if (!allResources.Any())
        {
            throw new ResourceNotFoundException(projectId, fullName);
        }

        return allResources.OrderByDescending(resource => resource.VersionIndex).First();
    }
}
