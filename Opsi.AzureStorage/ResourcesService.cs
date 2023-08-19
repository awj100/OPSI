﻿using Azure.Data.Tables;
using Opsi.AzureStorage.RowKeys;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.Common.Exceptions;

namespace Opsi.AzureStorage;

internal class ResourcesService : IResourcesService
{
    private const int StringComparisonMatch = 0;
    private const string TableName = "resources";
    private readonly IResourceRowKeyPolicies _rowKeyPolicies;
    private readonly ITableService _tableService;

    public ResourcesService(IResourceRowKeyPolicies rowKeyPolicies, ITableServiceFactory tableServiceFactory)
    {
        _rowKeyPolicies = rowKeyPolicies;
        _tableService = tableServiceFactory.Create(TableName);
    }

    public async Task DeleteResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await DeleteResourceAsync(resourceStorageInfo.ProjectId, resourceStorageInfo.RestOfPath!);
    }

    public async Task DeleteResourceAsync(ResourceTableEntity resource)
    {
        await DeleteResourceAsync(resource.ProjectId, resource.FullName!);
    }

    public async Task DeleteResourceAsync(Guid projectId, string fullName)
    {
        await _tableService.DeleteTableEntityAsync(projectId.ToString(), fullName);
    }

    public async Task<VersionInfo> GetCurrentVersionInfo(Guid projectId, string fullName)
    {
        var partitionKey = $"project_{projectId}";
        var rowKeyPrefix = _rowKeyPolicies.GetRowKeyPrefixForCount(projectId, fullName);
        var tableClient = _tableService.GetTableClient();

        var queryResults = tableClient.QueryAsync<ResourceTableEntity>($"PartitionKey eq '{partitionKey}' and RowKey ge '{rowKeyPrefix}'",
            select: new[]
            {
                nameof(ResourceTableEntity.VersionIndex),
                nameof(ResourceTableEntity.LockedTo)
            });

        var versionInfos = new List<VersionInfo>();

        await foreach (var queryResult in queryResults)
        {
            versionInfos.Add(new VersionInfo(queryResult.VersionIndex, queryResult.LockedTo));
        }

        var latestVersionInfo = versionInfos.OrderBy(versionInfo => versionInfo.Index).LastOrDefault();
        var lockedTo = latestVersionInfo.LockedTo.IsSome ? latestVersionInfo.LockedTo.Value : null;

        return new VersionInfo(latestVersionInfo.Index, lockedTo);
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

    public async Task<IReadOnlyCollection<ResourceTableEntity>> GetResourcesAsync(Guid projectId)
    {
        var key = projectId.ToString();
        var resources = new List<ResourceTableEntity>();

        var tableClient = _tableService.GetTableClient();
        var pageableResources = tableClient.QueryAsync<ResourceTableEntity>(x => x.PartitionKey == projectId.ToString());

        await foreach (var resource in pageableResources)
        {
            resources.Add(resource);
        }

        return resources;
    }

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var rowKeys = resourceStorageInfo.VersionInfo.Index == 1
            ? _rowKeyPolicies.GetRowKeysForCreate(resourceStorageInfo.ProjectId,
                                                  resourceStorageInfo.RestOfPath,
                                                  resourceStorageInfo.VersionInfo.Index)
            : _rowKeyPolicies.GetRowKeysForNewVersion(resourceStorageInfo.ProjectId,
                                                      resourceStorageInfo.RestOfPath,
                                                      resourceStorageInfo.VersionInfo.Index);

        var resources = rowKeys
            .Select(rowKey => new ResourceTableEntity
            {
                FullName = resourceStorageInfo.RestOfPath,
                LockedTo = resourceStorageInfo.VersionInfo.LockedTo.IsSome ? resourceStorageInfo.VersionInfo.LockedTo.Value : null,
                ProjectId = resourceStorageInfo.ProjectId,
                RowKey = rowKey,
                Username = resourceStorageInfo.Username,
                VersionId = resourceStorageInfo.VersionId,
                VersionIndex = resourceStorageInfo.VersionInfo.Index
            })
            .ToList();

        await _tableService.StoreTableEntitiesAsync(resources);
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

    private static async Task<ResourceTableEntity> GetResourceForLockOrUnlockAsync(TableClient tableClient, Guid projectId, string fullName)
    {
        var allResources = new List<ResourceTableEntity>();

        var key = projectId.ToString();
        var resources = new List<ResourceTableEntity>();
        var matchingResources = tableClient.QueryAsync<ResourceTableEntity>(resource => resource.PartitionKey == projectId.ToString()
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
