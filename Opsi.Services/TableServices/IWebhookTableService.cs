using Opsi.Services.InternalTypes;

namespace Opsi.Services.TableServices;

public interface IWebhookTableService
{
    Task<IReadOnlyCollection<InternalWebhookMessage>> GetUndeliveredAsync();

    Task StoreAsync(InternalWebhookMessage internalWebhookMessage);
}
