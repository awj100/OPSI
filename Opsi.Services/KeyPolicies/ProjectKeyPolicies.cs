using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.Services.KeyPolicies;

public class ProjectKeyPolicies : IProjectKeyPolicies
{
    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState)
    {
        return new[] {
            GetKeyPolicyByState(projectState, KeyOrders.Asc),
            GetKeyPolicyByState(projectState, KeyOrders.Desc)
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project)
    {
        var keyPolicies = new List<KeyPolicy>
        {
            GetKeyPolicyForGetById(project.Id)
        };

        keyPolicies.AddRange(GetKeyPoliciesByState(project.State));

        return keyPolicies;
    }

    public KeyPolicy GetKeyPolicyByState(string projectState, string keyOrder)
    {
        var uniquePart = GetUniqueOrderPart(keyOrder == KeyOrders.Asc);

        return new KeyPolicy($"projects_byState_{projectState}_{keyOrder}", new RowKey($"projects_byState_{keyOrder}_{uniquePart}", KeyPolicyQueryOperators.Equal));
    }

    public KeyPolicy GetKeyPolicyForGetById(Guid projectId)
    {
        return new KeyPolicy($"projects_byId", new RowKey($"projects_byId_{projectId}", KeyPolicyQueryOperators.Equal));
    }

    private static string GetUniqueOrderPart(bool forAscendingKey)
    {
        return string.Format("{0:D19}", forAscendingKey
                                            ? HiResDateTime.UtcNowTicks
                                            : DateTime.MaxValue.Ticks - HiResDateTime.UtcNowTicks);
    }
}
