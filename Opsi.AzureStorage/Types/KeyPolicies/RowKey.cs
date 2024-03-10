namespace Opsi.AzureStorage.Types;

public readonly struct RowKey
{
    /// <summary>
    /// Gets the equality operator indicating how the row key should be predicated.
    /// </summary>
    /// <example>
    /// If an entire row key is assigned to <see cref="Value"/> then <see cref="QueryOperator"/> should indicate <c>eq</c> ('equal').
    /// However, if the row key is a prefix then perhaps the <see cref="QueryOperator"/> should indicate <c>lt</c> ('less than').
    /// </example>
    public string QueryOperator { get; }

    /// <summary>
    /// Gets a string which can represent either a part of or the entire row key.
    /// </summary>
    public string Value { get; }

    public RowKey(string rowKey, string queryOperator) : this()
    {
        QueryOperator = queryOperator;
        Value = rowKey;
    }

    public override string ToString()
    {
        return $"{Value} | \"{QueryOperator}\"";
    }
}
