using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.Services.KeyPolicies;

public class ProjectKeyPolicies : KeyPoliciesBase, IProjectKeyPolicies
{
    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState)
    {
        return new[] {
            GetKeyPolicyByState(projectState, KeyOrders.Asc),
            GetKeyPolicyByState(projectState, KeyOrders.Desc)
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForUserAssignment(Guid projectId, string assigneeUsername)
    {
        return new[] {
            GetKeyPolicyByProjectForUserAssignment(projectId, assigneeUsername),
            GetKeyPolicyByUserForUserAssignment(projectId, assigneeUsername, KeyOrders.Asc),
            GetKeyPolicyByUserForUserAssignment(projectId, assigneeUsername, KeyOrders.Desc)
        };
    }

    public KeyPolicy GetKeyPolicyByState(string projectState, string keyOrder)
    {
        var uniqueOrderPart = GetUniqueOrderPart(keyOrder == KeyOrders.Asc);

        return new KeyPolicy($"projects_byState_{projectState}_{keyOrder}", new RowKey($"projects_byState_{keyOrder}_{uniqueOrderPart}", KeyPolicyQueryOperators.Equal));
    }

    public KeyPolicy GetKeyPolicyForGetById(Guid projectId)
    {
        return new KeyPolicy($"projects_byId", new RowKey($"projects_byId_{projectId}", KeyPolicyQueryOperators.Equal));
    }

    public KeyPolicy GetKeyPolicyByProjectForUserAssignment(Guid projectId, string assigneeUsername)
    {
        return new KeyPolicy($"projects_{projectId}", new RowKey($"assignment_byProjectAndUser_{projectId}_{assigneeUsername}", KeyPolicyQueryOperators.Equal));
    }

    public KeyPolicy GetKeyPolicyByUserForUserAssignment(Guid projectId, string assigneeUsername, string keyOrder)
    {
        var uniqueOrderPart = GetUniqueOrderPart(keyOrder == KeyOrders.Asc);

        return new KeyPolicy($"assignments_{assigneeUsername}", new RowKey($"assignment_byUserAndProject_{keyOrder}_{uniqueOrderPart}_{assigneeUsername}_{projectId}", KeyPolicyQueryOperators.Equal));
    }
}
