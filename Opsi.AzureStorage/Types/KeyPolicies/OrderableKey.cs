namespace Opsi.AzureStorage.Types.KeyPolicies;

public readonly struct OrderableKey
{
    public string KeyOrder { get; }

    public string Value { get; }

    public OrderableKey(string value, string keyOrder)
    {
        KeyOrder = keyOrder;
        Value = value;
    }
}
