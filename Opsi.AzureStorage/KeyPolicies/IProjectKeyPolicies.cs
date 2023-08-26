using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectKeyPolicies
{
    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Project project);

    KeyPolicy GetKeyPolicyForGet(Guid projectId);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForGetByState(string projectState);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStoreByState(string projectState);
}
