using Microsoft.Extensions.Logging;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Middleware;

internal abstract class MiddlewareExceptionHandlingBase
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<MiddlewareExceptionHandlingBase> _log;

    public MiddlewareExceptionHandlingBase(IErrorQueueService errorQueueService, ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _log = loggerFactory.CreateLogger<MiddlewareExceptionHandlingBase>();
    }

    protected async Task HandleErrorAsync(Exception exception)
    {
        try
        {
            if (exception is AggregateException aggregateException)
            {
                foreach(var aggregatedException in aggregateException.InnerExceptions)
                {
                    await _errorQueueService.ReportAsync(aggregatedException);
                }

                return;
            }

            await _errorQueueService.ReportAsync(exception);
        }
        catch (Exception errorQueueException)
        {
            _log.LogCritical(errorQueueException, "Unable to queue an unhandled exception from the exception-handling middleware.");
        }
    }
}
