using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

internal class KeyPolicyFilterGeneration : IKeyPolicyFilterGeneration
{
    public string ToFilter(KeyPolicy keyPolicy)
    {
        var partitionKey = keyPolicy.PartitionKey;

        return $"PartitionKey eq '{partitionKey}' and RowKey {keyPolicy.RowKey.QueryOperator} '{keyPolicy.RowKey.Value}'";
    }

    public string ToPartitionKeyFilter(KeyPolicy keyPolicy)
    {
        return $"PartitionKey eq '{keyPolicy.PartitionKey}'";
    }
}
