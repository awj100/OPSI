using System.Text;
using System.Web;
using Opsi.AzureStorage.RowKeys;

namespace Opsi.Services.RowKeys;

public class ResourceRowKeyPolicies : IResourceRowKeyPolicies
{
    public string GetRowKeyPrefixForCount(Guid projectId, string fullName)
    {
        return GetRowKeyPrefixForVersioning(projectId, fullName);
    }

    public IReadOnlyCollection<string> GetRowKeysForCreate(Guid projectId, string fullName, int versionIndex)
    {
        return new string[]
        {
            // This key will be left after all versioning has been deleted, and is a simple record of the resource.
            GetRowKeyPrefix(projectId, fullName),

            // This key is version-specific, and allows determination of the next version.
            $"{GetRowKeyPrefixForVersioning(projectId, fullName)}{versionIndex}"
        };
    }

    public IReadOnlyCollection<string> GetRowKeysForNewVersion(Guid projectId, string fullName, int versionIndex)
    {
        return new string[]
        {
            // This key is version-specific, and allows determination of the next version.
            $"{GetRowKeyPrefixForVersioning(projectId, fullName)}{versionIndex}"
        };
    }

    public IReadOnlyCollection<string> GetRowKeysForUserAssignment(Guid projectId, string fullName, string username)
    {
        var modifiedFulllName = GetNumericallySubstitutedString(fullName);
        var safeFullName = GetFullNameAsRowKey(modifiedFulllName);

        return new string[]
        {
            $"resource_byProjectAndAssignedUser_{projectId}_{username}_{safeFullName}"
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
        var modifiedFulllName = GetNumericallySubstitutedString(fullName);
        var safeFullName = GetFullNameAsRowKey(modifiedFulllName);

        return $"resource_byProject_{projectId}_{safeFullName}";
    }

    private static string GetRowKeyPrefixForVersioning(Guid projectId, string fullName)
    {
        return $"{GetRowKeyPrefix(projectId, fullName)}_v";
    }
}
