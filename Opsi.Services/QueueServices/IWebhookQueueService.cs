using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.QueueServices;

public interface IWebhookQueueService
{
    Task QueueWebhookMessageAsync(WebhookMessage webhookMessage, string remoteUri);

    Task QueueWebhookMessageAsync(InternalWebhookMessage internalWebhookMessage);
}