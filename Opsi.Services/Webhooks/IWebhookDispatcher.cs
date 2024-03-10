using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.Webhooks;

public interface IWebhookDispatcher
{
    Task<WebhookDispatchResponse> AttemptDeliveryAsync(WebhookMessage webhookMessage, Uri remoteUri, Dictionary<string, object>? customProps);
}
