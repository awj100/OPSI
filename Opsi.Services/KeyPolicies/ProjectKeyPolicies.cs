using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.Services.KeyPolicies;

public class ProjectKeyPolicies : IProjectKeyPolicies
{
    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState, Guid? projectId = null)
    {
        var projectIdAsString = projectId.HasValue ? projectId.ToString() : null;
        var equalityOperator = projectId.HasValue ? KeyPolicyQueryOperators.Equal : KeyPolicyQueryOperators.GreaterThanOrEqual;

        return new[] {
            new KeyPolicy($"projects_byState_{projectState}_{KeyOrders.Asc}", new RowKey($"projects_byState_asc_{projectState}_{projectIdAsString}", equalityOperator)),

            new KeyPolicy($"projects_byState_{projectState}_{KeyOrders.Desc}", new RowKey( $"projects_byState_desc_{projectState}_{projectIdAsString}", equalityOperator))
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project)
    {
        var keyPolicies = new List<KeyPolicy>
        {
            GetKeyPolicyForGetById(project.Id)
        };

        keyPolicies.AddRange(GetKeyPoliciesByState(project.State, project.Id));

        return keyPolicies;
    }

    public KeyPolicy GetKeyPolicyForGetById(Guid projectId)
    {
        return new KeyPolicy($"projects_byId", new RowKey($"projects_byId_{projectId}", KeyPolicyQueryOperators.Equal));
    }
}
