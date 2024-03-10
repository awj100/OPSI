using Opsi.AzureStorage;
using Opsi.Pocos;

namespace Opsi.Services.QueueServices;

internal class WebhookQueueService : IWebhookQueueService
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly IQueueService _queueService;

    public WebhookQueueService(IQueueServiceFactory queueServiceFactory, IErrorQueueService errorQueueService)
    {
        _errorQueueService = errorQueueService;
        _queueService = queueServiceFactory.Create(Constants.QueueNames.ConsumerWebhookSpecification);
    }

    public async Task QueueWebhookMessageAsync(WebhookMessage webhookMessage, ConsumerWebhookSpecification? webhookSpec)
    {
        if (webhookSpec == null || String.IsNullOrWhiteSpace(webhookSpec.Uri))
        {
            return;
        }

        var internalWebhookMessage = new InternalWebhookMessage(webhookMessage, webhookSpec);

        await QueueWebhookMessageAsync(internalWebhookMessage);
    }

    public async Task QueueWebhookMessageAsync(InternalWebhookMessage internalWebhookMessage)
    {
        if (String.IsNullOrWhiteSpace(internalWebhookMessage.WebhookSpecification?.Uri))
        {
            return;
        }

        try
        {
            await _queueService.AddMessageAsync(internalWebhookMessage);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
        }
    }
}
