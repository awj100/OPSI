using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorTableEntity : Error, ITableEntity
{
    private string _rowKey;

    public ErrorTableEntity(Error error)
    {
        _rowKey = Guid.NewGuid().ToString();

        var errorType = error.GetType();

        foreach (var propInfo in errorType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                          .Where(propInfo => propInfo.Name != nameof(ErrorTableEntity.InnerError)))
        {
            propInfo.SetValue(this, propInfo.GetValue(error));
        }
    }

    public ETag ETag { get; set; } = default!;

    public bool IsAcknowledged { get; set; }

    public string? ParentRowKey { get; set; }

    public string PartitionKey { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string RowKey
    {
        get => _rowKey;
        set
        {
            if (!Guid.TryParse(value, out var rowKeyAsGuid))
            {
                throw new ArgumentException($"{nameof(ErrorTableEntity)}.{nameof(RowKey)} must be in the format of a GUID.");
            }

            _rowKey = rowKeyAsGuid.ToString();
        }
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;
}
