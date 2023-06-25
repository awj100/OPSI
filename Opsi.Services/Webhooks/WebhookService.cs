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

    public async Task AttemptDeliveryAndRecordAsync(InternalCallbackMessage internalCallbackMessage)
    {
        const string absoluteUriPrefix = "http";

        // Don't sent the InternalCallbackMessage.
        var callbackMessage = internalCallbackMessage.ToCallbackMessage();

        // Obtain a Uri object from the CallbackMessage.RemoteUri string.
        if (String.IsNullOrWhiteSpace(internalCallbackMessage.RemoteUri)
            || !internalCallbackMessage.RemoteUri.StartsWith(absoluteUriPrefix)
            || !Uri.TryCreate(internalCallbackMessage.RemoteUri, UriKind.Absolute, out var remoteUri))
        {
            return;
        }

        // Dispatch the message.
        var isSuccessfullyDispatched = await _webhookDispatcher.AttemptDeliveryAsync(callbackMessage, remoteUri);

        // Record whether successful.
        internalCallbackMessage.IsDelivered = isSuccessfullyDispatched;

        // If unsuccessful, update the FailureCount property.
        if (!isSuccessfullyDispatched)
        {
            internalCallbackMessage.IncrementFailureCount();
        }

        // Store the InternalCallbackMessage object.
        //  - This should overwrite any previous entry for this InternalCallbackMessage because PartitionKey and RowKey are unchanged.
        await _webhookTableService.StoreAsync(internalCallbackMessage);
    }

    public async Task DispatchUndeliveredAsync()
    {
        var undeliveredInternalCallbackMessages = await _webhookTableService.GetUndeliveredAsync();

        var reAttemptTasks = undeliveredInternalCallbackMessages.Select(AttemptDeliveryAndRecordAsync);

        await Task.WhenAll(reAttemptTasks);
    }
}
