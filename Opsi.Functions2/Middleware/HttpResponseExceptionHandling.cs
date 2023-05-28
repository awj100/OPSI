using System.Net;
using System.Net.Mime;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Opsi.Services;

namespace Opsi.Functions2.Middleware;

internal sealed class HttpResponseExceptionHandling : MiddlewareExceptionHandlingBase, IFunctionsWorkerMiddleware
{
    public HttpResponseExceptionHandling(IErrorQueueService errorQueueService, ILoggerFactory loggerFactory) : base(errorQueueService, loggerFactory)
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

            await SendErrorResponseAsync(context);
        }
    }

    private static OutputBindingData<HttpResponseData> GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return context.GetOutputBindings<HttpResponseData>()
                      .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");
#pragma warning restore CS8603 // Possible null reference return.
    }

    private static async Task SendErrorResponseAsync(FunctionContext context)
    {
        const string httpResponseErrorOccurred = "An error occurred and has been logged.";

        var httpReqData = await context.GetHttpRequestDataAsync();

        if (httpReqData != null)
        {
            var newHttpResponse = httpReqData.CreateResponse(HttpStatusCode.InternalServerError);
            await newHttpResponse.WriteStringAsync(httpResponseErrorOccurred);
            newHttpResponse.Headers.Add(HeaderNames.ContentType, MediaTypeNames.Text.Plain);

            var invocationResult = context.GetInvocationResult();

            var httpOutputBindingFromMultipleOutputBindings = GetHttpOutputBindingFromMultipleOutputBinding(context);
            if (httpOutputBindingFromMultipleOutputBindings is not null)
            {
                httpOutputBindingFromMultipleOutputBindings.Value = newHttpResponse;
            }
            else
            {
                invocationResult.Value = newHttpResponse;
            }
        }
    }
}
