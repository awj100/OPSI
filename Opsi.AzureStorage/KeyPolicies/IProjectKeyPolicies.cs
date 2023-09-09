using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectKeyPolicies
{
    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project);

    KeyPolicy GetKeyPolicyByState(string projectState, string keyOrder);

    KeyPolicy GetKeyPolicyForGetById(Guid projectId);
}
