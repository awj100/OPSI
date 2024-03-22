using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

internal class KeyPolicyFilterGeneration : IKeyPolicyFilterGeneration
{
    // TODO: Move FilterStringComparison out of this class.
    public static class FilterStringComparison
    {
        public static readonly string And = "and";
        public static readonly string Or = "or";
    }

    public string ToFilter(KeyPolicy keyPolicy)
    {
        var partitionKey = keyPolicy.PartitionKey;

        return $"PartitionKey eq '{partitionKey}' and RowKey {keyPolicy.RowKey.QueryOperator} '{keyPolicy.RowKey.Value}'";
    }

    public string ToFilter(IEnumerable<KeyPolicy> keyPolicies, string filterStringComparison)
    {
        return String.Join($" {filterStringComparison} ", $"({keyPolicies.Select(ToFilter)})");
    }

    public string ToPartitionKeyFilter(KeyPolicy keyPolicy)
    {
        return $"PartitionKey eq '{keyPolicy.PartitionKey}'";
    }
}
