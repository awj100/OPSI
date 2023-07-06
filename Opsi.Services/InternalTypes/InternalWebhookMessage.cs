using System.Web;
using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.Services.InternalTypes;

public class InternalWebhookMessage : WebhookMessage, ITableEntity
{
    public InternalWebhookMessage()
    {
    }

    public InternalWebhookMessage(WebhookMessage webhookMessage, string remoteUri)
    {
        foreach (var propInfo in webhookMessage.GetType().GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
        {
            propInfo.SetValue(this, propInfo.GetValue(webhookMessage));
        }

        RemoteUri = remoteUri;
    }

    public ETag ETag { get; set; }

    public int FailureCount { get; set; } = default;

    public bool IsDelivered { get; set; } = default;

    public string? LastFailureReason { get; set; } = default;

    public string PartitionKey
    {
        get => ProjectId.ToString();
        set
        {
            if (!Guid.TryParse(value, out var projectId))
            {
                throw new ArgumentException($"Invalid value for {nameof(InternalWebhookMessage)}.{nameof(PartitionKey)}: {value}.");
            }

            ProjectId = projectId;
        }
    }

    public string? RemoteUri { get; set; }

    public string RowKey
    {
        get => Id.ToString();
        set
        {
            if (!Guid.TryParse(value, out var id))
            {
                throw new ArgumentException($"Invalid value for {nameof(InternalWebhookMessage)}.{nameof(RowKey)}: {value}.");
            }

            Id = id;
        }
    }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public void IncrementFailureCount()
    {
        FailureCount++;
    }

    public WebhookMessage ToWebhookMessage()
    {
        var webhookMessage = new WebhookMessage();

        foreach (var propInfo in webhookMessage.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            propInfo.SetValue(webhookMessage, propInfo.GetValue(this));
        }

        return webhookMessage;
    }
}
