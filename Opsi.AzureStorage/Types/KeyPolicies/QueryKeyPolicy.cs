namespace Opsi.AzureStorage.Types.KeyPolicies;

public readonly struct QueryKeyPolicy
{
    /// <summary>
    /// The type cn be either <c>string</c> or <see cref="OrderableKey"/>.
    /// </summary>
    public object PartitionKey { get; }

    public RowKey RowKey { get; }

    public QueryKeyPolicy(OrderableKey partitionKey, RowKey rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    public QueryKeyPolicy(string partitionKey, RowKey rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }
}
