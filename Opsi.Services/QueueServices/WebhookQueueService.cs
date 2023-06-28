using Opsi.AzureStorage;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.QueueServices;

internal class WebhookQueueService : IWebhookQueueService
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly IQueueService _queueService;

    public WebhookQueueService(IQueueServiceFactory queueServiceFactory, IErrorQueueService errorQueueService)
    {
        _errorQueueService = errorQueueService;
        _queueService = queueServiceFactory.Create(Constants.QueueNames.Webhook);
    }

    public async Task QueueWebhookMessageAsync(WebhookMessage webhookMessage, string remoteUri)
    {
        var internalWebhookMessage = new InternalWebhookMessage(webhookMessage, remoteUri);

        await QueueWebhookMessageAsync(internalWebhookMessage);
    }

    public async Task QueueWebhookMessageAsync(InternalWebhookMessage internalWebhookMessage)
    {
        if (String.IsNullOrWhiteSpace(internalWebhookMessage.RemoteUri))
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
