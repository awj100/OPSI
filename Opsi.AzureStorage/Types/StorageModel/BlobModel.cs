namespace Opsi.AzureStorage.Types.StorageModel;

public abstract class BlobModel
{
    public BlobModel(string fullName, string type)
    {
        FullName = fullName;
        Type = type;
    }

    public string FullName { get; }

    public string? Name => GetName();

    public string Type { get; }

    protected abstract string? GetName();
}
