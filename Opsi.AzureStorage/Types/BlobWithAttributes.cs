namespace Opsi.AzureStorage;

public class BlobWithAttributes(string name)
{
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    public string Name { get; set; } = name;

    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    public override string ToString()
    {
        return $"\"{Name}\" (tag count: {Tags.Count} | metadata count: {Metadata.Count})";
    }
}
