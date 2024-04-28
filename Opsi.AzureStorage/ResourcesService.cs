﻿using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

internal class ResourcesService(IResourceKeyPolicies _keyPolicies,
                        ITableServiceFactory _tableServiceFactory,
                        IKeyPolicyFilterGeneration _keyPolicyFilterGeneration) : IResourcesService
{
    private const string TableName = "resources";
    private readonly ITableService _tableService = _tableServiceFactory.Create(TableName);

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
        var keyPolicy = _keyPolicies.GetKeyPolicyForResourceCount(projectId, fullName);
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicy);

        var queryResults = tableClient.QueryAsync<ResourceVersionTableEntity>(filter,
            select: new[]
            {
                nameof(ResourceVersionTableEntity.VersionIndex),
                nameof(ResourceVersionTableEntity.Username)
            });

        var versionInfos = new List<VersionInfo>();

        await foreach (var queryResult in queryResults)
        {
            versionInfos.Add(new VersionInfo(queryResult.VersionIndex, queryResult.Username));
        }

        var latestVersionInfo = versionInfos.OrderBy(versionInfo => versionInfo.Index).LastOrDefault();
        var lockedTo = latestVersionInfo.AssignedTo.IsSome ? latestVersionInfo.AssignedTo.Value : null;

        return new VersionInfo(latestVersionInfo.Index, lockedTo);
    }

    public async Task<IReadOnlyCollection<ResourceTableEntity>> GetResourcesAsync(Guid projectId)
    {
        var key = projectId.ToString();
        var resources = new List<ResourceTableEntity>();

        var tableClient = _tableService.TableClient.Value;
        var pageableResources = tableClient.QueryAsync<ResourceTableEntity>(x => x.PartitionKey == projectId.ToString());

        await foreach (var resource in pageableResources)
        {
            resources.Add(resource);
        }

        return resources;
    }

    public async Task StoreResourceAsync(VersionedResourceStorageInfo versionedResourceStorageInfo)
    {
        var keyPolicies = versionedResourceStorageInfo.VersionInfo.Index == 1
            ? _keyPolicies.GetKeyPoliciesForStore(versionedResourceStorageInfo.ProjectId,
                                            versionedResourceStorageInfo.RestOfPath,
                                            versionedResourceStorageInfo.VersionInfo.Index)
            : _keyPolicies.GetKeyPoliciesForNewVersion(versionedResourceStorageInfo.ProjectId,
                                                versionedResourceStorageInfo.RestOfPath,
                                                versionedResourceStorageInfo.VersionInfo.Index);

        var resources = (from keyPolicy in keyPolicies
                         select new ResourceTableEntity
                         {
                             FullName = versionedResourceStorageInfo.RestOfPath,
                             AssignedTo = versionedResourceStorageInfo.VersionInfo.AssignedTo.IsSome ? versionedResourceStorageInfo.VersionInfo.AssignedTo.Value : null,
                             PartitionKey = keyPolicy.PartitionKey,
                             ProjectId = versionedResourceStorageInfo.ProjectId,
                             RowKey = keyPolicy.RowKey.Value,
                             CreatedBy = versionedResourceStorageInfo.Username
                             //  VersionId = resourceStorageInfo.VersionId,
                             //  VersionIndex = resourceStorageInfo.VersionInfo.Index
                         }).ToList();

        await _tableService.StoreTableEntitiesAsync(resources);
    }
}
