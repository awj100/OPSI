using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.ErrorReporting.Services;
using Opsi.Pocos;

namespace Opsi.Functions;

public class ErrorQueueHandler : FunctionBase
{
    private readonly IErrorService _errorService;
    private readonly ILogger<ErrorQueueHandler> _logger;

    public ErrorQueueHandler(IErrorService errorService, ILoggerFactory loggerFactory)
    {
        _errorService = errorService;
        _logger = loggerFactory.CreateLogger<ErrorQueueHandler>();
    }

    [Function(nameof(ErrorQueueHandler))]
    public async Task Run([QueueTrigger(QueueNames.Error, Connection = ConfigNameConnectionString)] Error error)
    {
        _logger.LogInformation($"{nameof(ErrorQueueHandler)} triggered by an exception in {error.Origin}.");

        await _errorService.ReportAsync(error);
    }
}
