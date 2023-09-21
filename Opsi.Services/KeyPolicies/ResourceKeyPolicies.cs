using System.Web;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.Services.KeyPolicies;

public class ResourceKeyPolicies : KeyPoliciesBase, IResourceKeyPolicies
{
    public KeyPolicy GetKeyPolicyForResourceCount(Guid projectId, string fullName)
    {
        return new KeyPolicy($"projects_{projectId}", new RowKey(GetRowKeyPrefixForVersioning(projectId, fullName), KeyPolicyQueryOperators.GreaterThan));
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForNewVersion(Guid projectId, string fullName, int versionIndex)
    {
        return new KeyPolicy[]
        {
            // This row key is version-specific, and allows determination of the next version.
            new KeyPolicy($"projects_{projectId}", new RowKey($"{GetRowKeyPrefixForVersioning(projectId, fullName)}{versionIndex}", KeyPolicyQueryOperators.Equal))
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Guid projectId, string fullName, int versionIndex)
    {
        var partitionKey = $"projects_{projectId}";

        return new KeyPolicy[]
        {
            // This key will be left after all versioning has been deleted, and is a simple record of the resource.
            new KeyPolicy(partitionKey, new RowKey(GetRowKeyPrefix(projectId, fullName), KeyPolicyQueryOperators.LessThan)),

            // This key is version-specific, and allows determination of the next version.
            new KeyPolicy(partitionKey, new RowKey($"{GetRowKeyPrefixForVersioning(projectId, fullName)}{versionIndex}", KeyPolicyQueryOperators.Equal))
        };
    }

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForUserAssignment(Guid projectId, string fullName, string assigneeUsername)
    {
        var safeFullName = GetFullNameAsRowKey(fullName);

        return new KeyPolicy[]
        {
            new KeyPolicy($"assignments_{assigneeUsername}", new RowKey($"assignment_byProjectAndResource_{projectId}_{safeFullName}", KeyPolicyQueryOperators.Equal))
        };
    }

    private static string GetFullNameAsRowKey(string fullName)
    {
        return HttpUtility.UrlEncode(fullName);
    }

    private static string GetRowKeyPrefix(Guid projectId, string fullName)
    {
        var safeFullName = GetFullNameAsRowKey(fullName);

        return $"resource_byProject_{projectId}_{safeFullName}";
    }

    private static string GetRowKeyPrefixForVersioning(Guid projectId, string fullName)
    {
        var modifiedFulllName = GetAlphanumericallySubstitutedString(fullName);
        var safeFullName = GetFullNameAsRowKey(modifiedFulllName);

        return $"versionedResource_byProject_{projectId}_{safeFullName}_v";
    }
}
