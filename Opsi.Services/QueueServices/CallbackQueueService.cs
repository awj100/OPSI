using Opsi.AzureStorage;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.QueueServices;

internal class CallbackQueueService : ICallbackQueueService
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly IQueueService _queueService;

    public CallbackQueueService(IQueueServiceFactory queueServiceFactory, IErrorQueueService errorQueueService)
    {
        _errorQueueService = errorQueueService;
        _queueService = queueServiceFactory.Create(Constants.QueueNames.Callback);
    }

    public async Task QueueCallbackAsync(CallbackMessage callbackMessage, string remoteUri)
    {
        var internalCallbackMessage = new InternalCallbackMessage(callbackMessage, remoteUri);

        await QueueCallbackAsync(internalCallbackMessage);
    }

    public async Task QueueCallbackAsync(InternalCallbackMessage internalCallbackMessage)
    {
        if (String.IsNullOrWhiteSpace(internalCallbackMessage.RemoteUri))
        {
            return;
        }

        try
        {
            await _queueService.AddMessageAsync(internalCallbackMessage);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
        }
    }
}
