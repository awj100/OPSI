using Opsi.Pocos;

namespace Opsi.Services.QueueHandlers;

public interface IZippedQueueHandler
{
    Task RetrieveAndHandleUploadAsync(Manifest manifest);
}