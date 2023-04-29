namespace Opsi.Services.Types.StorageModel;

public class FolderModel : BlobModel
{
    private const string _type = "Folder";

    public FolderModel(string fullName, ICollection<BlobModel> items) : base(fullName, _type)
    {
        Items = items;
    }

    public ICollection<BlobModel> Items { get; }

    protected override string? GetName()
    {
        const char pathSeparator = '/';
        return FullName.TrimEnd(pathSeparator).Split(pathSeparator).LastOrDefault();
    }

    public override string ToString()
    {
        return $"{FullName} ({Items.Count} item{(Items.Count == 1 ? String.Empty : "s")})";
    }
}
