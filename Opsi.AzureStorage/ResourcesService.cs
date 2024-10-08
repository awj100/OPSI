﻿using Azure.Data.Tables;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage;

internal class ResourcesService(IResourceKeyPolicies _resourceKeyPolicies,
                                IProjectKeyPolicies _projectKeyPolicies,
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
        var keyPolicy = _resourceKeyPolicies.GetKeyPolicyForResourceCount(projectId, fullName);
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

    public async Task<IReadOnlyCollection<IGrouping<string, VersionedResourceStorageInfo>>> GetHistoryAsync(Guid projectId, string username)
    {
        var keyPolicies = new List<KeyPolicy>
                          {
                              _resourceKeyPolicies.GetKeyPolicyForResourceHistory(projectId),
                              _projectKeyPolicies.GetKeyPolicyByProjectForUserAssignment(projectId, username)
                          };
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicies, KeyPolicyFilterGeneration.FilterStringComparison.Or);

        var queryResults = tableClient.QueryAsync<TableEntity>(filter,
                                                               select: [
                                                                   nameof(ResourceVersionTableEntity.FullName),
                                                                   nameof(ResourceVersionTableEntity.VersionId),
                                                                   nameof(ResourceVersionTableEntity.VersionIndex),
                                                                   nameof(ResourceVersionTableEntity.Username),
                                                                   nameof(ResourceVersionTableEntity.EntityType)
                                                               ]);

        var versions = new List<VersionedResourceStorageInfo>();

        // In order for this project to have been available to (i.e., a resource has been assigned to) the specified user, there
        // should be at least one result whose EntityType is 'UserAssignmentTableEntity'.
        var isUserAssigned = false;

        await foreach (var queryResult in queryResults)
        {
            if (queryResult.GetString(nameof(ResourceTableEntity.EntityType)) == typeof(ResourceTableEntity).Name)
            {
                var versionIndex = queryResult.GetInt32(nameof(ResourceVersionTableEntity.VersionIndex)) ?? 0;
                var resourceUsername = queryResult.GetString(nameof(ResourceVersionTableEntity.Username));
                var versionInfo = new VersionInfo(versionIndex, resourceUsername);
                var fullName = queryResult.GetString(nameof(ResourceVersionTableEntity.FullName));
                versions.Add(new VersionedResourceStorageInfo(projectId,
                                                              fullName,
                                                              Stream.Null,
                                                              resourceUsername,
                                                              versionInfo));
            }
            else if (queryResult.GetString(nameof(UserAssignmentTableEntity.EntityType)) == typeof(UserAssignmentTableEntity).Name && !isUserAssigned)
            {
                isUserAssigned = true;
            }
        }

        return isUserAssigned
                ? versions.GroupBy(version => version.RestOfPath).ToList()
                : [];
    }

    public async Task<IReadOnlyCollection<VersionedResourceStorageInfo>> GetHistoryAsync(Guid projectId, string fullName, string username)
    {
        var keyPolicies = new List<KeyPolicy>
                          {
                              _resourceKeyPolicies.GetKeyPolicyForResourceHistory(projectId, fullName),
                              _projectKeyPolicies.GetKeyPolicyByProjectForUserAssignment(projectId, username)
                          };
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicies, KeyPolicyFilterGeneration.FilterStringComparison.Or);

        var queryResults = tableClient.QueryAsync<TableEntity>(filter,
                                                               select: [
                                                                   nameof(ResourceVersionTableEntity.VersionId),
                                                                   nameof(ResourceVersionTableEntity.VersionIndex),
                                                                   nameof(ResourceVersionTableEntity.Username),
                                                                   nameof(ResourceVersionTableEntity.EntityType)
                                                               ]);

        var versions = new List<VersionedResourceStorageInfo>();

        // In order for this project to have been available to (i.e., a resource has been assigned to) the specified user, there
        // should be at least one result whose EntityType is 'UserAssignmentTableEntity'.
        var isUserAssigned = false;

        await foreach (var queryResult in queryResults)
        {
            if (queryResult.GetString(nameof(ResourceTableEntity.EntityType)) == typeof(ResourceTableEntity).Name)
            {
                var versionIndex = queryResult.GetInt32(nameof(ResourceVersionTableEntity.VersionIndex)) ?? 0;
                var resourceUsername = queryResult.GetString(nameof(ResourceVersionTableEntity.Username));
                var versionInfo = new VersionInfo(versionIndex, resourceUsername);
                versions.Add(new VersionedResourceStorageInfo(projectId,
                                                              fullName,
                                                              Stream.Null,
                                                              resourceUsername,
                                                              versionInfo));
            }
            else if (queryResult.GetString(nameof(UserAssignmentTableEntity.EntityType)) == typeof(UserAssignmentTableEntity).Name && !isUserAssigned)
            {
                isUserAssigned = true;
            }
        }

        return isUserAssigned
                ? versions
                : [];
    }

    public async Task<IReadOnlyCollection<IGrouping<string, VersionedResourceStorageInfo>>> GetHistoryForAdminAsync(Guid projectId)
    {
        var keyPolicy = _resourceKeyPolicies.GetKeyPolicyForResourceHistory(projectId);
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicy);

        var queryResults = tableClient.QueryAsync<ResourceVersionTableEntity>(filter,
                                                                              select: [
                                                                                nameof(ResourceVersionTableEntity.FullName),
                                                                                nameof(ResourceVersionTableEntity.VersionId),
                                                                                nameof(ResourceVersionTableEntity.VersionIndex),
                                                                                nameof(ResourceVersionTableEntity.Username)
                                                                              ]);

        var versions = new List<VersionedResourceStorageInfo>();

        await foreach (var queryResult in queryResults)
        {
            var versionInfo = new VersionInfo(queryResult.VersionIndex, queryResult.Username);
            versions.Add(new VersionedResourceStorageInfo(projectId,
                                                          queryResult.FullName,
                                                          Stream.Null,
                                                          queryResult.Username!,
                                                          versionInfo));
        }

        return versions.GroupBy(version => version.RestOfPath).ToList();
    }

    public async Task<IReadOnlyCollection<VersionedResourceStorageInfo>> GetHistoryForAdminAsync(Guid projectId, string fullName)
    {
        var keyPolicy = _resourceKeyPolicies.GetKeyPolicyForResourceHistory(projectId, fullName);
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicy);

        var queryResults = tableClient.QueryAsync<ResourceVersionTableEntity>(filter,
                                                                              select: [
                                                                                nameof(ResourceVersionTableEntity.VersionId),
                                                                                nameof(ResourceVersionTableEntity.VersionIndex),
                                                                                nameof(ResourceVersionTableEntity.Username)
                                                                              ]);

        var versions = new List<VersionedResourceStorageInfo>();

        await foreach (var queryResult in queryResults)
        {
            var versionInfo = new VersionInfo(queryResult.VersionIndex, queryResult.Username);
            versions.Add(new VersionedResourceStorageInfo(projectId,
                                                          fullName,
                                                          Stream.Null,
                                                          queryResult.Username!,
                                                          versionInfo));
        }

        return versions;
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

    public async Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername)
    {
        var keyPolicy = _resourceKeyPolicies.GetKeyPoliciesForUserAssignment(projectId, fullName, requestingUsername).First();
        var tableClient = _tableService.TableClient.Value;
        var pageableEntities = tableClient.QueryAsync<ResourceTableEntity>(x => x.PartitionKey == keyPolicy.PartitionKey && x.RowKey == x.RowKey);

        return await pageableEntities.AnyAsync();
    }

    public async Task StoreResourceAsync(VersionedResourceStorageInfo versionedResourceStorageInfo)
    {
        var keyPolicies = versionedResourceStorageInfo.VersionInfo.Index == 1
            ? _resourceKeyPolicies.GetKeyPoliciesForStore(versionedResourceStorageInfo.ProjectId,
                                            versionedResourceStorageInfo.RestOfPath,
                                            versionedResourceStorageInfo.VersionInfo.Index)
            : _resourceKeyPolicies.GetKeyPoliciesForNewVersion(versionedResourceStorageInfo.ProjectId,
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
                             Username = versionedResourceStorageInfo.Username
                            //  VersionId = resourceStorageInfo.VersionId,
                            //  VersionIndex = resourceStorageInfo.VersionInfo.Index
                         }).ToList();

        await _tableService.StoreTableEntitiesAsync(resources);
    }
}
