namespace Opsi.AzureStorage.Types.KeyPolicies;

public readonly struct KeyPolicy
{
    public string PartitionKey { get; }

    public RowKey RowKey { get; }

    public KeyPolicy(string partitionKey, RowKey rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    public override string ToString()
    {
        return $"{PartitionKey} | {RowKey}";
    }
}
