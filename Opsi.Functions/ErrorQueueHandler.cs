using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.ErrorReporting.Services;
using Opsi.Pocos;

namespace Opsi.Functions;

public class ErrorQueueHandler
{
    private readonly IErrorService _errorService;

    public ErrorQueueHandler(IErrorService errorService)
    {
        _errorService = errorService;
    }

    [FunctionName(nameof(ErrorQueueHandler))]
    public async Task Run([QueueTrigger(QueueNames.Error, Connection = "AzureWebJobsStorage")] Error error, ILogger log)
    {
        log.LogInformation($"{nameof(ErrorQueueHandler)} triggered by an exception in {error.Origin}.");

        await _errorService.ReportAsync(error);
    }
}
