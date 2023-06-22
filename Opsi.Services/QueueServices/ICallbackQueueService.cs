using Opsi.Pocos;

namespace Opsi.Services.QueueServices;

public interface ICallbackQueueService
{
    Task QueueCallbackAsync(CallbackMessage callbackMessage);
}