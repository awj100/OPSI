using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.QueueServices;

public interface IWebhookQueueService
{
    Task QueueWebhookMessageAsync(WebhookMessage webhookMessage, ConsumerWebhookSpecification webhookSpec);

    Task QueueWebhookMessageAsync(InternalWebhookMessage internalWebhookMessage);
}