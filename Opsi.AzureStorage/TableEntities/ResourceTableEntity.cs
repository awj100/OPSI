using System.Reflection;
using System.Web;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json.Linq;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class ResourceTableEntity : Resource, ITableEntity
{
    private Guid _projectId;
    private string? _rowKey;

    public ETag ETag { get; set; } = default!;

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

    public string RowKey
    {
        get => _rowKey ?? HttpUtility.UrlEncode(FullName ?? String.Empty);
        set => _rowKey = value;
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public Resource ToResource()
    {
        var resource = new Resource();

        foreach (var propInfo in resource.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            propInfo.SetValue(resource, propInfo.GetValue(this));
        }

        return resource;
    }

    public override string ToString()
    {
        return $"{FullName} ({VersionIndex})";
    }
}
