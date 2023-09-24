using System.Reflection;
using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class UserAssignmentTableEntity : UserAssignment, ITableEntity
{
    public string EntityType { get; set; } = typeof(UserAssignmentTableEntity).Name;

    public int EntityVersion { get; set; } = 1;

    public ETag ETag { get; set; }

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; }

    public UserAssignment ToUserAssignment()
    {
        return this;
    }

    public static UserAssignmentTableEntity FromUserAssignment(UserAssignment userAssignment, KeyPolicy keyPolicy)
    {
        var tableEntity = new UserAssignmentTableEntity();

        foreach (var propInfo in userAssignment.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            propInfo.SetValue(tableEntity, propInfo.GetValue(userAssignment));
        }

        tableEntity.PartitionKey = keyPolicy.PartitionKey;
        tableEntity.RowKey = keyPolicy.RowKey.Value;

        return tableEntity;
    }

    public static IReadOnlyCollection<UserAssignmentTableEntity> FromUserAssignment(UserAssignment userAssignment, IEnumerable<KeyPolicy> keyPolicies)
    {
        return keyPolicies.Select(keyPolicy => FromUserAssignment(userAssignment, keyPolicy)).ToList();
    }
}
