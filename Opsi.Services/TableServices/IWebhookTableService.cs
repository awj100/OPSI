using Opsi.Services.InternalTypes;

namespace Opsi.Services.TableServices;

public interface IWebhookTableService
{
    Task<IReadOnlyCollection<InternalCallbackMessage>> GetUndeliveredAsync();

    Task StoreAsync(InternalCallbackMessage internalCallbackMessage);
}
