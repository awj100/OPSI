using Opsi.AzureStorage.RowKeys;
using Opsi.Pocos;

namespace Opsi.Services.RowKeys;

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
        return String.Format("{0:D19}", forAscendingKey
                                            ? DateTime.UtcNow.Ticks
                                            : DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
    }
}
