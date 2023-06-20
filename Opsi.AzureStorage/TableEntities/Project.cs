using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public record Project : ITableEntity
{
    public Project()
    {
    }

    public Project(InternalManifest internalManifest)
    {
        CallbackUri = internalManifest.CallbackUri;
        Id = internalManifest.ProjectId;
        Name = internalManifest.PackageName;
        Username = internalManifest.Username;
        InternalManifest = internalManifest;
    }

    public string CallbackUri { get; set; } = default!;

    public ETag ETag { get; set; } = default!;

    public Guid Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string PartitionKey { get; set; } = DateTime.UtcNow.ToString("yyyyMMdd");

    public string RowKey
    {
        get => Id.ToString();
        set => Id = Guid.Parse(value);
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public string Username { get; set; } = default!;

    // TODO: Is this needed?
    public InternalManifest InternalManifest { get; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
