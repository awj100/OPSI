using System.Reflection;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class InternalWebhookMessageTableEntity : InternalWebhookMessageBase, ITableEntity
{
    public InternalWebhookMessageTableEntity()
    {
    }

    public static InternalWebhookMessageTableEntity FromInternalWebhookMessage(InternalWebhookMessage internalWebhookMessage)
    {
        var tableEntity = new InternalWebhookMessageTableEntity();

        foreach(var propInfo in typeof(InternalWebhookMessageBase).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            propInfo.SetValue(tableEntity, propInfo.GetValue(internalWebhookMessage));
        }

        try
        {
            tableEntity.SerialisedWebhookCustomProps = internalWebhookMessage?.WebhookSpecification?.CustomProps != null
                ? JsonSerializer.Serialize(internalWebhookMessage?.WebhookSpecification?.CustomProps)
                : String.Empty;
        }
        catch (Exception exception)
        {
            tableEntity.SerialisedWebhookCustomProps = $"Unable to serialise {nameof(InternalWebhookMessage)}.{nameof(InternalWebhookMessage.WebhookSpecification)}.{nameof(InternalWebhookMessage.WebhookSpecification.CustomProps)}: {exception.Message}";
        }

        tableEntity.WebhookUri = internalWebhookMessage?.WebhookSpecification?.Uri ?? String.Empty;

        return tableEntity;
    }

    public InternalWebhookMessage ToInternalWebhookMessage()
    {
        var internalWebhookMessage = new InternalWebhookMessage();

        foreach (var propInfo in typeof(InternalWebhookMessageBase).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            propInfo.SetValue(internalWebhookMessage, propInfo.GetValue(this));
        }

        internalWebhookMessage.WebhookSpecification = new ConsumerWebhookSpecification { Uri = WebhookUri };

        try
        {
            internalWebhookMessage.WebhookSpecification.CustomProps = JsonSerializer.Deserialize<Dictionary<string, object>>(SerialisedWebhookCustomProps) ?? new Dictionary<string, object>(0);
        }
        catch (Exception exception)
        {
            internalWebhookMessage.WebhookSpecification.CustomProps = new Dictionary<string, object> { {exception.GetType().Name,exception.Message} };
        }

        return internalWebhookMessage;
    }

    public ETag ETag { get; set; }

    public string PartitionKey
    {
        get => ProjectId.ToString();
        set
        {
            if (!Guid.TryParse(value, out var projectId))
            {
                throw new ArgumentException($"Invalid value for {nameof(InternalWebhookMessageTableEntity)}.{nameof(PartitionKey)}: {value}.");
            }

            ProjectId = projectId;
        }
    }

    public string RowKey
    {
        get => Id.ToString();
        set
        {
            if (!Guid.TryParse(value, out var id))
            {
                throw new ArgumentException($"Invalid value for {nameof(InternalWebhookMessageTableEntity)}.{nameof(RowKey)}: {value}.");
            }

            Id = id;
        }
    }

    public DateTimeOffset? Timestamp { get; set; }

    public string WebhookUri { get; set; } = default!;

    public string SerialisedWebhookCustomProps { get; set; } = default!;
}
