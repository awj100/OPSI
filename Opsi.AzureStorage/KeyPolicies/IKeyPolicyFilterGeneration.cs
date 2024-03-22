using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IKeyPolicyFilterGeneration
{
    string ToFilter(KeyPolicy keyPolicy);

    string ToFilter(IEnumerable<KeyPolicy> keyPolicies, string filterStringComparison);

    string ToPartitionKeyFilter(KeyPolicy keyPolicy);
}
