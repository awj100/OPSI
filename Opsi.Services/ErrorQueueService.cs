using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Pocos;

namespace Opsi.Services;

internal class ErrorQueueService : IErrorQueueService
{
    private readonly ILogger _logger;
    private readonly IQueueService _queueService;

    public ErrorQueueService(ILoggerFactory loggerFactory, IQueueServiceFactory queueServiceFactory)
    {
        _logger = loggerFactory.CreateLogger<ErrorQueueService>();
        _queueService = queueServiceFactory.Create(Constants.QueueNames.Error);
    }

    public async Task ReportAsync(Exception exception,
                                  LogLevel logLevel = LogLevel.Error,
                                  [CallerMemberName] string exceptionOrigin = "")
    {
        _logger.Log(logLevel, exception, exceptionOrigin);

        var error = new Error(exceptionOrigin, exception);

        try
        {
            await _queueService.AddMessageAsync(error);
        }
        catch (Exception queueServiceException)
        {
            _logger.LogCritical(queueServiceException, "UNABLE TO QUEUE ERROR!");
        }
    }
}
