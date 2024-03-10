using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.Webhooks;

namespace Opsi.Functions.Functions.Webhooks;

public class WebhookDispatchFunction
{
    private readonly ILogger _logger;
    private readonly IWebhookService _webhookService;

    public WebhookDispatchFunction(IWebhookService webhookService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<WebhookDispatchFunction>();
        _webhookService = webhookService;
    }

    [Function("WebhookDispatchFunction")]
    public async Task Run([QueueTrigger(QueueNames.ConsumerWebhookSpecification, Connection = "AzureWebJobsStorage")] InternalWebhookMessage internalWebhookMessage)
    {
        _logger.LogInformation($"{nameof(WebhookDispatchFunction)} triggered for first-time delivery of message.");

        await _webhookService.AttemptDeliveryAndRecordAsync(internalWebhookMessage);
    }
}
