using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class ResourceVersionTableEntity : ResourceVersion, ITableEntity
{
    public string EntityType { get; set; } = typeof(ResourceVersionTableEntity).Name;

    public int EntityVersion { get; set; } = 1;

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public ResourceVersion ToResourceVersion()
    {
        return new ResourceVersion
        {
            FullName = FullName,
            ProjectId = ProjectId,
            Username = Username,
            VersionId = VersionId,
            VersionIndex = VersionIndex
        };
    }

    public override string ToString()
    {
        return $"{FullName} (v{VersionIndex} | {Username})";
    }

    public static ResourceVersionTableEntity FromResourceVersion(ResourceVersion resourceVersion, string partitionKey, string rowKey)
    {
        return new ResourceVersionTableEntity
        {
            FullName = resourceVersion.FullName,
            PartitionKey = partitionKey,
            ProjectId = resourceVersion.ProjectId,
            RowKey = rowKey,
            Username = resourceVersion.Username,
            VersionId = resourceVersion.VersionId,
            VersionIndex = resourceVersion.VersionIndex
        };
    }

    public static IReadOnlyCollection<ResourceVersionTableEntity> FromResourceVersion(ResourceVersion resourceVersion, Func<ResourceVersion, IReadOnlyCollection<KeyPolicy>> keyPolicyResolvers)
    {
        return keyPolicyResolvers(resourceVersion).Select(keyPolicy => FromResourceVersion(resourceVersion,
                                                                             keyPolicy.PartitionKey,
                                                                             keyPolicy.RowKey.Value)).ToList();
    }
}
