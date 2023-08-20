using Opsi.Pocos;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectRowKeyPolicies
{
    IReadOnlyCollection<string> GetRowKeysForCreate(Project project);
}
