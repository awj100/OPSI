using Opsi.AzureStorage.Types.StorageModel;

namespace Opsi.AzureStorage.Types.StorageModel;

public class FileModel : BlobModel
{
    private const string _type = "File";

    public FileModel(string fullName, Stream content) : base(fullName, _type)
    {
        Content = content;
    }

    public Stream Content { get; }

    protected override string GetName()
    {
        return Path.GetFileName(FullName);
    }

    public override string ToString()
    {
        return FullName;
    }
}
