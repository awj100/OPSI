using Opsi.Pocos;

namespace Opsi.Services.Webhooks;

public interface IWebhookDispatcher
{
    Task<bool> AttemptDeliveryAsync(WebhookMessage webhookMessage, Uri remoteUri);
}
