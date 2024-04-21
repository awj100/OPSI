using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class ResourceTableEntity : Resource, ITableEntity
{
    public string EntityType { get; set; } = typeof(ResourceTableEntity).Name;

    public int EntityVersion { get; set; } = 1;

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public Resource ToResource()
    {
        return new Resource
        {
            AssignedBy = AssignedBy,
            AssignedOnUtc = AssignedOnUtc,
            AssignedTo = AssignedTo,
            FullName = FullName,
            ProjectId = ProjectId,
            CreatedBy = CreatedBy
        };
    }

    public override string ToString()
    {
        return FullName ?? "[Unnamed]";
    }

    public static ResourceTableEntity FromResource(Resource resource, string partitionKey, string rowKey)
    {
        return new ResourceTableEntity
        {
            AssignedBy = resource.AssignedBy,
            AssignedOnUtc = resource.AssignedOnUtc,
            AssignedTo = resource.AssignedTo,
            FullName = resource.FullName,
            PartitionKey = partitionKey,
            ProjectId = resource.ProjectId,
            RowKey = rowKey,
            CreatedBy = resource.CreatedBy
        };
    }

    public static IReadOnlyCollection<ResourceTableEntity> FromResource(Resource resource, Func<Resource, IReadOnlyCollection<KeyPolicy>> keyPolicyResolvers)
    {
        return keyPolicyResolvers(resource).Select(keyPolicy => FromResource(resource,
                                                                             keyPolicy.PartitionKey,
                                                                             keyPolicy.RowKey.Value)).ToList();
    }
}
