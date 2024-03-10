using Opsi.Pocos;

namespace Opsi.Services.Webhooks;

public interface IWebhookService
{
    Task AttemptDeliveryAndRecordAsync(InternalWebhookMessage internalWebhookMessage);

    Task DispatchUndeliveredAsync();
}
