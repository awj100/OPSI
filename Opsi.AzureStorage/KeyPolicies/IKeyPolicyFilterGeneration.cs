using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IKeyPolicyFilterGeneration
{
    string ToFilter(KeyPolicy keyPolicy);
}
