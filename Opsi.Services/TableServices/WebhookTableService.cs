using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;

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

        var tableClient = _tableService.TableClient.Value;

        var queryResults = tableClient.QueryAsync<InternalWebhookMessageTableEntity>($"{nameof(InternalWebhookMessageTableEntity.IsDelivered)} eq false and {nameof(InternalWebhookMessageTableEntity.FailureCount)} lt {MaxFailureCount}",
                                                                                     maxResultsPerPage,
                                                                                     selectProps,
                                                                                     CancellationToken.None);

        await foreach (var queryResult in queryResults)
        {
            results.Add(queryResult.ToInternalWebhookMessage());
        }

        return results;
    }

    public async Task StoreAsync(InternalWebhookMessage internalWebhookMessage)
    {
        var tableEntity = InternalWebhookMessageTableEntity.FromInternalWebhookMessage(internalWebhookMessage);

        if (internalWebhookMessage.FailureCount > 1)
        {
            await _tableService.UpdateTableEntitiesAsync(tableEntity);
        }
        else
        {
            await _tableService.StoreTableEntitiesAsync(tableEntity);
        }
    }
}
