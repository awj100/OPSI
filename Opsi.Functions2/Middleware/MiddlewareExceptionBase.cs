using Microsoft.Extensions.Logging;
using Opsi.Services;

namespace Opsi.Functions2.Middleware;

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
            await _errorQueueService.ReportAsync(exception);
        }
        catch (Exception errorQueueException)
        {
            _log.LogCritical(errorQueueException, "Unable to queue an unhandled exception from the exception-handling middleware.");
        }
    }
}
