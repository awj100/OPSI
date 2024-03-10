using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class OrderedProjectTableEntity : OrderedProject, ITableEntity
{
    public string EntityType { get; set; } = typeof(OrderedProjectTableEntity).Name;

    public int EntityVersion { get; set; } = 1;

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public OrderedProject ToOrderedProject()
    {
        return new OrderedProject
        {
            Id = Id,
            Name = Name
        };
    }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }

    public static OrderedProjectTableEntity FromOrderedProject(OrderedProject orderedProject, string partitionKey, string rowKey)
    {
        return new OrderedProjectTableEntity
        {
            Id = orderedProject.Id,
            Name = orderedProject.Name,
            PartitionKey = partitionKey,
            RowKey = rowKey
        };
    }

    public static IReadOnlyCollection<OrderedProjectTableEntity> FromOrderedProject(OrderedProject orderedProject, Func<OrderedProject, IReadOnlyCollection<KeyPolicy>> keyPolicyResolvers)
    {
        return keyPolicyResolvers(orderedProject).Select(keyPolicy => FromOrderedProject(orderedProject,
                                                                                         keyPolicy.PartitionKey,
                                                                                         keyPolicy.RowKey.Value)).ToList();
    }

    public static OrderedProjectTableEntity FromProject(Project project, string partitionKey, string rowKey)
    {
        return new OrderedProjectTableEntity
        {
            Id = project.Id,
            Name = project.Name,
            PartitionKey = partitionKey,
            RowKey = rowKey
        };
    }

    public static OrderedProjectTableEntity FromProjectTableEntity(ProjectTableEntity projectTableEntity)
    {
        return new OrderedProjectTableEntity
        {
            Id = projectTableEntity.Id,
            Name = projectTableEntity.Name,
            PartitionKey = projectTableEntity.PartitionKey,
            RowKey = projectTableEntity.RowKey
        };
    }
}
