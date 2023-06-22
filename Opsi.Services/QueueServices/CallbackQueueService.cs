using Opsi.AzureStorage;
using Opsi.Pocos;

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

    public async Task QueueCallbackAsync(CallbackMessage callbackMessage)
    {
        try
        {
            await _queueService.AddMessageAsync(callbackMessage);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
        }
    }
}
