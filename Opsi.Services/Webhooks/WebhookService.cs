using Opsi.Services.InternalTypes;
using Opsi.Services.TableServices;

namespace Opsi.Services.Webhooks;

internal class WebhookService : IWebhookService
{
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly IWebhookTableService _webhookTableService;

    public WebhookService(IWebhookDispatcher webhookDispatcher, IWebhookTableService webhookTableService)
    {
        _webhookDispatcher = webhookDispatcher;
        _webhookTableService = webhookTableService;
    }

    public async Task AttemptDeliveryAndRecordAsync(InternalWebhookMessage internalWebhookMessage)
    {
        const string absoluteUriPrefix = "http";

        // Don't sent the InternalWebhookMessage.
        var webhookMessage = internalWebhookMessage.ToWebhookMessage();

        // Obtain a Uri object from the WebhookMessage.RemoteUri string.
        if (String.IsNullOrWhiteSpace(internalWebhookMessage.RemoteUri)
            || !internalWebhookMessage.RemoteUri.StartsWith(absoluteUriPrefix)
            || !Uri.TryCreate(internalWebhookMessage.RemoteUri, UriKind.Absolute, out var remoteUri))
        {
            return;
        }

        // Dispatch the message.
        var isSuccessfullyDispatched = await _webhookDispatcher.AttemptDeliveryAsync(webhookMessage, remoteUri);

        // Record whether successful.
        internalWebhookMessage.IsDelivered = isSuccessfullyDispatched;

        // If unsuccessful, update the FailureCount property.
        if (!isSuccessfullyDispatched)
        {
            internalWebhookMessage.IncrementFailureCount();
        }

        // Store the InternalWebhookMessage object.
        //  - This should overwrite any previous entry for this InternalWebhookMessage because PartitionKey and RowKey are unchanged.
        await _webhookTableService.StoreAsync(internalWebhookMessage);
    }

    public async Task DispatchUndeliveredAsync()
    {
        var undeliveredInternalWebhookMessages = await _webhookTableService.GetUndeliveredAsync();

        var reAttemptTasks = undeliveredInternalWebhookMessages.Select(AttemptDeliveryAndRecordAsync);

        await Task.WhenAll(reAttemptTasks);
    }
}
