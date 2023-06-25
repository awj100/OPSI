using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.QueueServices;

public interface ICallbackQueueService
{
    Task QueueCallbackAsync(CallbackMessage callbackMessage, string remoteUri);

    Task QueueCallbackAsync(InternalCallbackMessage internalCallbackMessage);
}