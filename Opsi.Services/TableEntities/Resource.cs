using System.Web;
using Azure;
using Azure.Data.Tables;

namespace Opsi.Services.TableEntities;

public record Resource : ITableEntity
{
    private string? _fullName;
    private Guid _projectId;

    public ETag ETag { get; set; } = default!;

    public string? FullName
    {
        get => _fullName;
        set
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Invalid value for {nameof(FullName)}: {(value == null ? "null" : $"\"{value}\"")}");
            }

            _fullName = value;
            RowKey = HttpUtility.UrlEncode(value);
        }
    }

    public string? LockedTo { get; set; } = default;

    public string PartitionKey { get; set; } = default!;

    public Guid ProjectId
    {
        get => _projectId;
        set
        {
            _projectId = value;
            PartitionKey = value.ToString();
        }
    }

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public string Username { get; set; } = default!;

    public int Version { get; set; } = default!;

    public override string ToString()
    {
        return $"{FullName} ({Version})";
    }
}
