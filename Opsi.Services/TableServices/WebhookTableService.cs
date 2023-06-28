using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
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
        const int maxResultsPerPage = 1;
        IEnumerable<string>? selectProps = null;
        var Results = new List<InternalWebhookMessage>();

        var tableClient = _tableService.GetTableClient();

        var queryResults = tableClient.QueryAsync<InternalWebhookMessage>($"{nameof(InternalWebhookMessage.IsDelivered)} eq false and {nameof(InternalWebhookMessage.FailureCount)} lt {MaxFailureCount}",
                                                                           maxResultsPerPage,
                                                                           selectProps,
                                                                           CancellationToken.None);

        await foreach (var queryResult in queryResults)
        {
            Results.Add(queryResult);
        }

        return Results;
    }

    public async Task StoreAsync(InternalWebhookMessage internalWebhookMessage)
    {
        await _tableService.StoreTableEntityAsync(internalWebhookMessage);
    }
}
