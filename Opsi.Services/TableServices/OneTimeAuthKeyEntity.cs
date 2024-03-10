using Azure;
using Azure.Data.Tables;

namespace Opsi.Services.TableServices;

public class OneTimeAuthKeyEntity : ITableEntity
{
    public OneTimeAuthKeyEntity()
    {
    }

    public OneTimeAuthKeyEntity(string username, string key)
    {
        PartitionKey = username;
        RowKey = key;
    }

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;
}

