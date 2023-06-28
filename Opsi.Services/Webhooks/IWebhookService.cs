using Opsi.Services.InternalTypes;

namespace Opsi.Services.Webhooks;

public interface IWebhookService
{
    Task AttemptDeliveryAndRecordAsync(InternalWebhookMessage internalWebhookMessage);

    Task DispatchUndeliveredAsync();
}
