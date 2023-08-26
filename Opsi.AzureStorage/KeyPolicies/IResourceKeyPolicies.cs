using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IResourceKeyPolicies
{
    KeyPolicy GetKeyPrefixForResourceCount(Guid projectId, string fullName);

    IReadOnlyCollection<KeyPolicy> GetKeysForCreate(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<KeyPolicy> GetKeysForNewVersion(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<KeyPolicy> GetKeysForUserAssignment(Guid projectId, string fullName, string username);
}
