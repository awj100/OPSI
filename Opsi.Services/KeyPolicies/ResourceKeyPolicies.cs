using System.Text;
using System.Web;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.Services.KeyPolicies;

public class ResourceKeyPolicies : IResourceKeyPolicies
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

    public IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForUserAssignment(Guid projectId, string fullName, string username)
    {
        var modifiedFulllName = GetNumericallySubstitutedString(fullName);
        var safeFullName = GetFullNameAsRowKey(modifiedFulllName);

        return new KeyPolicy[]
        {
            new KeyPolicy($"projects_{projectId}", new RowKey($"resource_byProjectAndAssignedUser_{projectId}_{username}_{safeFullName}", KeyPolicyQueryOperators.Equal))
        };
    }

    private static string GetFullNameAsRowKey(string fullName)
    {
        return HttpUtility.UrlEncode(fullName);
    }

    private static string GetNumericallySubstitutedString(string s)
    {
        var lowerInvariantS = s.ToLowerInvariant();

        const int charA = 'a';
        const int charZ = 'z';
        const int char0 = '0';
        const int char9 = '9';

        var newString = new StringBuilder();

        foreach (var c in lowerInvariantS)
        {
            if (c >= char0 && c <= char9)
            {
                newString.Append((char)(char0 + char9 - c));
            }
            else if (c >= charA && c <= charZ)
            {
                newString.Append((char)(charA + charZ - c));
            }

            newString.Append(c);
        }

        return newString.ToString();
    }

    private static string GetRowKeyPrefix(Guid projectId, string fullName)
    {
        var safeFullName = GetFullNameAsRowKey(fullName);

        return $"resource_byProject_{projectId}_{safeFullName}";
    }

    private static string GetRowKeyPrefixForVersioning(Guid projectId, string fullName)
    {
        var modifiedFulllName = GetNumericallySubstitutedString(fullName);
        var safeFullName = GetFullNameAsRowKey(modifiedFulllName);

        return $"versionedResource_byProject_{projectId}_{safeFullName}_v";
    }
}
