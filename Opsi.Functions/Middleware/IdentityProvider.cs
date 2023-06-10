using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Opsi.Functions.Middleware;

internal class IdentityProvider : IFunctionsWorkerMiddleware
{
    private const string ItemNameClaims = "Claims";
    private const string ItemNameUsername = "Username";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        context.Items.Add(ItemNameClaims, new List<string> { "Administrator" });
        context.Items.Add(ItemNameUsername, "user@test.com");

        await next(context);
    }
}
