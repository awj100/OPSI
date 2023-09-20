using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectKeyPolicies
{
    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState);

    KeyPolicy GetKeyPolicyByState(string projectState, string keyOrder);

    KeyPolicy GetKeyPolicyForGetById(Guid projectId);
}
