using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

internal class ResourcesService : IResourcesService
{
    private const string TableName = "resources";
    private readonly IResourceKeyPolicies _keyPolicies;
    private readonly IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;
    private readonly ITableService _tableService;

    public ResourcesService(IResourceKeyPolicies keyPolicies,
                            ITableServiceFactory tableServiceFactory,
                            IKeyPolicyFilterGeneration keyPolicyFilterGeneration)
    {
        _keyPolicies = keyPolicies;
        _keyPolicyFilterGeneration = keyPolicyFilterGeneration;
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
        var keyPolicy = _keyPolicies.GetKeyPolicyForResourceCount(projectId, fullName);
        var tableClient = _tableService.TableClient.Value;
        var filter = _keyPolicyFilterGeneration.ToFilter(keyPolicy);

        var queryResults = tableClient.QueryAsync<ResourceTableEntity>(filter,
            select: new[]
            {
                nameof(ResourceTableEntity.VersionIndex),
                nameof(ResourceTableEntity.AssignedTo)
            });

        var versionInfos = new List<VersionInfo>();

        await foreach (var queryResult in queryResults)
        {
            versionInfos.Add(new VersionInfo(queryResult.VersionIndex, queryResult.AssignedTo));
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

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var keyPolicies = resourceStorageInfo.VersionInfo.Index == 1
            ? _keyPolicies.GetKeyPoliciesForStore(resourceStorageInfo.ProjectId,
                                            resourceStorageInfo.RestOfPath,
                                            resourceStorageInfo.VersionInfo.Index)
            : _keyPolicies.GetKeyPoliciesForNewVersion(resourceStorageInfo.ProjectId,
                                                resourceStorageInfo.RestOfPath,
                                                resourceStorageInfo.VersionInfo.Index);

        var resources = (from keyPolicy in keyPolicies
                         select new ResourceTableEntity
                         {
                             FullName = resourceStorageInfo.RestOfPath,
                             AssignedTo = resourceStorageInfo.VersionInfo.AssignedTo.IsSome ? resourceStorageInfo.VersionInfo.AssignedTo.Value : null,
                             PartitionKey = keyPolicy.PartitionKey,
                             ProjectId = resourceStorageInfo.ProjectId,
                             RowKey = keyPolicy.RowKey.Value,
                             Username = resourceStorageInfo.Username,
                             VersionId = resourceStorageInfo.VersionId,
                             VersionIndex = resourceStorageInfo.VersionInfo.Index
                         }).ToList();

        await _tableService.StoreTableEntitiesAsync(resources);
    }
}
