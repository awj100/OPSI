using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;

namespace Opsi.Functions.Middleware;

internal class AdministratorEnforcement : IFunctionsWorkerMiddleware
{
    private readonly Func<FunctionContext, IUserProvider> _funcUserProvider;
    private readonly ILoggerFactory _loggerFactory;

    public AdministratorEnforcement(Func<FunctionContext, IUserProvider> funcUserProvider, ILoggerFactory loggerFactory)
    {
        _funcUserProvider = funcUserProvider;
        _loggerFactory = loggerFactory;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.FunctionDefinition.EntryPoint.StartsWith("Opsi.Functions.Functions.Administrator."))
        {
            var userProvider = _funcUserProvider(context);
            if (!userProvider.IsAdministrator.Value)
            {
                var logger = _loggerFactory.CreateLogger<AdministratorEnforcement>();
                logger.LogWarning($"Attempt to access Administrator-level function ({context.FunctionDefinition.EntryPoint}) by non-Administrator ({userProvider.Username.Value}).");

                throw new UnauthenticatedException();
            }
        }

        await next(context);
    }
}
