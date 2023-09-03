using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectKeyPolicies
{
    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState, Guid? projectId = null);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project);

    KeyPolicy GetKeyPolicyForGetById(Guid projectId);
}
