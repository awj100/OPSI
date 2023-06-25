using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Services.InternalTypes;
using Opsi.Services.Webhooks;

namespace Opsi.Functions.Webhooks
{
    public class WebhookQueueHandlerFunction
    {
        private readonly ILogger _logger;
        private readonly IWebhookService _webhookService;

        public WebhookQueueHandlerFunction(IWebhookService webhookService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebhookDispatchFunction>();
            _webhookService = webhookService;
        }

        [Function("WebhookQueueHandlerFunction")]
        public async Task Run([TimerTrigger("20 * * * * *")] MyInfo myTimer)
        {
            _logger.LogInformation($"{nameof(WebhookQueueHandlerFunction)} for redelivery at {DateTime.Now}");

            await _webhookService.DispatchUndeliveredAsync();
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}

