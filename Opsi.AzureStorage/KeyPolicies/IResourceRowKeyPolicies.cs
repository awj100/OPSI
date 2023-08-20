namespace Opsi.AzureStorage.KeyPolicies;

public interface IResourceRowKeyPolicies
{
    string GetRowKeyPrefixForCount(Guid projectId, string fullName);

    IReadOnlyCollection<string> GetRowKeysForCreate(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<string> GetRowKeysForNewVersion(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<string> GetRowKeysForUserAssignment(Guid projectId, string fullName, string username);
}
