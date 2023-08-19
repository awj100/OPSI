using Opsi.Pocos;

namespace Opsi.AzureStorage.RowKeys;

public interface IProjectRowKeyPolicies
{
    IReadOnlyCollection<string> GetRowKeysForCreate(Project project);
}
