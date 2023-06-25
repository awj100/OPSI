using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.Services.InternalTypes;

public class InternalCallbackMessage : CallbackMessage, ITableEntity
{
    // Default constructor, required for JSON deserialisation.
    public InternalCallbackMessage()
    {
    }

    public InternalCallbackMessage(CallbackMessage callbackMessage, string remoteUri)
    {
        foreach (var propInfo in callbackMessage.GetType().GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
        {
            propInfo.SetValue(this, propInfo.GetValue(callbackMessage));
        }

        RemoteUri = remoteUri;
    }

    public int FailureCount { get; set; } = default;

    public bool IsDelivered { get; set; } = default;

    public string PartitionKey
    {
        get => ProjectId.ToString();
        set
        {
            if (!Guid.TryParse(value, out var projectId))
            {
                throw new ArgumentException($"Invalid value for {nameof(InternalCallbackMessage)}.{nameof(PartitionKey)}: {value}.");
            }

            ProjectId = projectId;
        }
    }

    public string? RemoteUri { get; set; }

    public string RowKey
    {
        get => Status;
        set => Status = value;
    }

    public ETag ETag { get; set; }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public void IncrementFailureCount()
    {
        FailureCount = FailureCount + 1;
    }

    public CallbackMessage ToCallbackMessage()
    {
        var callbackMessage = new CallbackMessage();

        foreach (var propInfo in callbackMessage.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            propInfo.SetValue(callbackMessage, propInfo.GetValue(this));
        }

        return callbackMessage;
    }
}
