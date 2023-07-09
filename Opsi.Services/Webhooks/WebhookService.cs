using Opsi.Pocos;
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
        if (String.IsNullOrWhiteSpace(internalWebhookMessage.WebhookSpecification.Uri)
            || !internalWebhookMessage.WebhookSpecification.Uri.StartsWith(absoluteUriPrefix)
            || !Uri.TryCreate(internalWebhookMessage.WebhookSpecification.Uri, UriKind.Absolute, out var remoteUri))
        {
            return;
        }

        // Dispatch the message.
        var webhookDispatchResponse = await _webhookDispatcher.AttemptDeliveryAsync(webhookMessage, remoteUri, internalWebhookMessage.WebhookSpecification.CustomProps);

        // Record whether successful.
        internalWebhookMessage.IsDelivered = webhookDispatchResponse.IsSuccessful;
        internalWebhookMessage.LastFailureReason = webhookDispatchResponse.FailureReason;

        // If unsuccessful, update the FailureCount property.
        if (!internalWebhookMessage.IsDelivered)
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
