using Azure;
using Azure.Data.Tables;

namespace Opsi.AzureStorage.TableEntities;

public record Project : ITableEntity
{
    public string CallbackUri { get; set; } = default!;

    public ETag ETag { get; set; } = default!;

    public Guid Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string PartitionKey
    {
        get => Id.ToString();
        set => Id = Guid.Parse(value);
    }

    public string RowKey
    {
        get => Id.ToString();
        set => Id = Guid.Parse(value);
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public string Username { get; set; } = default!;

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
