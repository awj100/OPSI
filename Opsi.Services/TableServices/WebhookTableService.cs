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

    public async Task<IReadOnlyCollection<InternalCallbackMessage>> GetUndeliveredAsync()
    {
        const int maxResultsPerPage = 1;
        IEnumerable<string>? selectProps = null;
        var callbackResults = new List<InternalCallbackMessage>();

        var tableClient = _tableService.GetTableClient();

        var queryResults = tableClient.QueryAsync<InternalCallbackMessage>($"{nameof(InternalCallbackMessage.IsDelivered)} eq false and {nameof(InternalCallbackMessage.FailureCount)} lt {MaxFailureCount}",
                                                                           maxResultsPerPage,
                                                                           selectProps,
                                                                           CancellationToken.None);

        await foreach (var queryResult in queryResults)
        {
            callbackResults.Add(queryResult);
        }

        return callbackResults;
    }

    public async Task StoreAsync(InternalCallbackMessage internalCallbackMessage)
    {
        await _tableService.StoreTableEntityAsync(internalCallbackMessage);
    }
}
