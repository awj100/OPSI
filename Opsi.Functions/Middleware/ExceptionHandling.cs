using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Opsi.Services;

namespace Opsi.Functions.Middleware;

internal class ExceptionHandling : MiddlewareExceptionHandlingBase, IFunctionsWorkerMiddleware
{
    public ExceptionHandling(IErrorQueueService errorQueueService, ILoggerFactory loggerFactory) : base(errorQueueService, loggerFactory)
    {
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }
}
