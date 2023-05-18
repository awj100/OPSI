using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueHandlers;

namespace Opsi.Functions.PackageHandlers;

public class ZippedQueueHandler
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly IZippedQueueHandler _zippedQueueHandler;

    public ZippedQueueHandler(IZippedQueueHandler zippedQueueHandler, IErrorQueueService errorQueueService)
    {
        _errorQueueService = errorQueueService;
        _zippedQueueHandler = zippedQueueHandler;
    }

    [FunctionName(nameof(ZippedQueueHandler))]
    public async Task Run([QueueTrigger($"manifests-{QueueHandlerNames.Zipped}", Connection = "AzureWebJobsStorage")] Manifest manifest, ILogger log)
    {
        log.LogInformation($"{nameof(ZippedQueueHandler)} triggered for \"{manifest.PackageName}\".");

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
