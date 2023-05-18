using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueHandlers;

namespace Opsi.Functions2.QueueHandlers;

public class ZippedQueueHandler
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger _logger;
    private readonly IZippedQueueHandler _zippedQueueHandler;

    public ZippedQueueHandler(IZippedQueueHandler zippedQueueHandler,
                              IErrorQueueService errorQueueService,
                              ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ZippedQueueHandler>();
        _zippedQueueHandler = zippedQueueHandler;
    }

    [Function(nameof(ZippedQueueHandler))]
    public async Task Run([QueueTrigger($"manifests-{QueueHandlerNames.Zipped}", Connection = "AzureWebJobsStorage")] Manifest manifest)
    {
        _logger.LogInformation($"{nameof(ZippedQueueHandler)} triggered for \"{manifest.PackageName}\".");

        try
        {
            await _zippedQueueHandler.RetrieveAndHandleUploadAsync(manifest);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
        }
    }
}
