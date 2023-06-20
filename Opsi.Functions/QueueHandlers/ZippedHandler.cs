using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueHandlers;

namespace Opsi.Functions.QueueHandlers;

public class ZippedHandler : FunctionBase
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger _logger;
    private readonly IZippedQueueHandler _zippedQueueHandler;

    public ZippedHandler(IZippedQueueHandler zippedQueueHandler,
                         IErrorQueueService errorQueueService,
                         ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ZippedHandler>();
        _zippedQueueHandler = zippedQueueHandler;
    }

    [Function(nameof(ZippedHandler))]
    public async Task Run([QueueTrigger($"manifests-{QueueHandlerNames.Zipped}", Connection = ConfigNameConnectionString)] InternalManifest internalManifest)
    {
        _logger.LogInformation($"{nameof(ZippedHandler)} triggered for \"{internalManifest.PackageName}\".");

        try
        {
            await _zippedQueueHandler.RetrieveAndHandleUploadAsync(internalManifest);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
        }
    }
}
