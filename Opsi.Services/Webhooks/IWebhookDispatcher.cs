using Opsi.Pocos;

namespace Opsi.Services.Webhooks;

public interface IWebhookDispatcher
{
    Task<bool> AttemptDeliveryAsync(CallbackMessage callbackMessage, Uri remoteUri);
}
