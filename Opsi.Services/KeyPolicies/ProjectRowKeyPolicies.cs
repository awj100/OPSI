using Opsi.AzureStorage.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.Services.KeyPolicies;

public class ProjectRowKeyPolicies : IProjectRowKeyPolicies
{
    public IReadOnlyCollection<string> GetRowKeysForCreate(Project project)
    {
        return new string[]
        {
            $"project_byStateAsc_{project.State}_{GetRowKeyUniquePart(forAscendingKey: true)}",
            $"project_byStateDesc_{project.State}_{GetRowKeyUniquePart(forAscendingKey: false)}"
        };
    }

    private static string GetRowKeyUniquePart(bool forAscendingKey)
    {
        return string.Format("{0:D19}", forAscendingKey
                                            ? DateTime.UtcNow.Ticks
                                            : DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
    }
}
