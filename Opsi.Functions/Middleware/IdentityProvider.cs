using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Net.Http.Headers;
using Opsi.Common.Exceptions;
using Opsi.Services.Auth;

namespace Opsi.Functions.Middleware;

internal class IdentityProvider : IFunctionsWorkerMiddleware
{
    private readonly IAuthService _authService;

    public IdentityProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var authHeader = await GetAuthHeaderAsync(context);

        if (!await _authService.TrySetAuthenticationContextItems(authHeader, context.Items))
        {
            throw new UnauthenticatedException();
        }

        await next(context);
    }

    private static async Task<string?> GetAuthHeaderAsync(FunctionContext context)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData == null)
        {
            return null;
        }

        if (!requestData.Headers.Contains(HeaderNames.Authorization))
        {
            return null;
        }

        return requestData!.Headers
            .FirstOrDefault(header => header.Key == HeaderNames.Authorization).Value
            .FirstOrDefault();
    }
}
