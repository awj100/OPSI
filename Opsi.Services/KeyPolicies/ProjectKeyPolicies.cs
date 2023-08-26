using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.Services.KeyPolicies;

public class ProjectKeyPolicies : IProjectKeyPolicies
{
    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project)
    {
        var keyPolicies = new List<KeyPolicy>();

        keyPolicies.AddRange(GetKeyPoliciesForGetByState(project.State));
        keyPolicies.Add(GetKeyPolicyForGet(project.Id));

        return keyPolicies;
    }

    public KeyPolicy GetKeyPolicyForGet(Guid projectId)
    {
        return new KeyPolicy($"projects_byId", new RowKey($"projects_byId_{projectId}", KeyPolicyQueryOperators.Equal));
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForGetByState(string projectState)
    {
        return new[] {
            new KeyPolicy($"projects_byState_{projectState}_{KeyOrders.Asc}", new RowKey($"projects_byState_asc_{projectState}_", KeyPolicyQueryOperators.GreaterThanOrEqual)),

            new KeyPolicy($"projects_byState_{projectState}_{KeyOrders.Desc}", new RowKey( $"projects_byState_desc_{projectState}_", KeyPolicyQueryOperators.LessThanOrEqual))
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStoreByState(string projectState)
    {
        var getByStatePolicies = GetKeyPoliciesForGetByState(projectState);
        var storeByStatePolicies = new List<KeyPolicy>(getByStatePolicies.Count);

        foreach (var keyPolicy in getByStatePolicies)
        {
            var uniquePart = GetRowKeyUniquePart(forAscendingKey: true);
            var partitionKey = keyPolicy.PartitionKey;

            storeByStatePolicies.Add(new KeyPolicy(partitionKey, new RowKey($"{keyPolicy.RowKey}{uniquePart}", KeyPolicyQueryOperators.Equal)));
        }

        return storeByStatePolicies;
    }

    private static string GetRowKeyUniquePart(bool forAscendingKey)
    {
        return string.Format("{0:D19}", forAscendingKey
                                            ? DateTime.UtcNow.Ticks
                                            : DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
    }
}
