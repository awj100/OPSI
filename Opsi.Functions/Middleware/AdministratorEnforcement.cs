using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;

namespace Opsi.Functions.Middleware;

internal class AdministratorEnforcement(Func<FunctionContext, IUserProvider> _funcUserProvider, ILoggerFactory _loggerFactory) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.FunctionDefinition.EntryPoint.StartsWith("Opsi.Functions.Functions.Administrator."))
        {
            var userProvider = _funcUserProvider(context);
            if (!userProvider.IsAdministrator)
            {
                var logger = _loggerFactory.CreateLogger<AdministratorEnforcement>();
                logger.LogWarning($"Attempt to access Administrator-level function ({context.FunctionDefinition.EntryPoint}) by non-Administrator ({userProvider.Username}).");

                throw new UnauthenticatedException();
            }
        }

        await next(context);
    }
}
