using Opsi.AzureStorage;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.TableServices;

public class WebhookTableService : IWebhookTableService
{
    private const int MaxFailureCount = 5;
    private const string TableName = "webhooks";
    private readonly ITableService _tableService;

    public WebhookTableService(ITableServiceFactory tableServiceFactory)
    {
        _tableService = tableServiceFactory.Create(TableName);
    }

    public async Task<IReadOnlyCollection<InternalWebhookMessage>> GetUndeliveredAsync()
    {
        const int maxResultsPerPage = 100;
        IEnumerable<string>? selectProps = null;
        var results = new List<InternalWebhookMessage>();

        var tableClient = _tableService.GetTableClient();

        var queryResults = tableClient.QueryAsync<InternalWebhookMessage>($"{nameof(InternalWebhookMessage.IsDelivered)} eq false and {nameof(InternalWebhookMessage.FailureCount)} lt {MaxFailureCount}",
                                                                           maxResultsPerPage,
                                                                           selectProps,
                                                                           CancellationToken.None);

        await foreach (var queryResult in queryResults)
        {
            results.Add(queryResult);
        }

        return results;
    }

    public async Task StoreAsync(InternalWebhookMessage internalWebhookMessage)
    {
        if (internalWebhookMessage.FailureCount > 1)
        {
            await _tableService.UpdateTableEntityAsync(internalWebhookMessage);
        }
        else
        {
            await _tableService.StoreTableEntityAsync(internalWebhookMessage);
        }
    }
}
