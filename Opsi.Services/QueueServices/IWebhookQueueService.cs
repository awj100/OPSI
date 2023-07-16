using Opsi.Pocos;

namespace Opsi.Services.QueueServices;

public interface IWebhookQueueService
{
    Task QueueWebhookMessageAsync(WebhookMessage webhookMessage, ConsumerWebhookSpecification? webhookSpec);

    Task QueueWebhookMessageAsync(InternalWebhookMessage internalWebhookMessage);
}