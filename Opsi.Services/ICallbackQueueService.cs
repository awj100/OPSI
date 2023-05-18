using Opsi.Pocos;

namespace Opsi.Services;

public interface ICallbackQueueService
{
    Task QueueCallbackAsync(CallbackMessage callbackMessage);
}