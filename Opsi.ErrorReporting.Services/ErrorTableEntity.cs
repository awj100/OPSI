using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorTableEntity : Error, ITableEntity
{
    public ErrorTableEntity(Error error)
    {
        var errorType = error.GetType();

        foreach (var propInfo in errorType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            propInfo.SetValue(this, propInfo.GetValue(error));
        }
    }

    public ETag ETag { get; set; } = default!;

    public bool IsAcknowledged { get; set; }

    public string PartitionKey { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string RowKey
    {
        get => Guid.NewGuid().ToString();
        set => Guid.Parse(value);
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;
}
