using Opsi.AzureStorage;

namespace Opsi.Services.TableServices;

internal class OneTimeAuthKeysTableService : IOneTimeAuthKeysTableService
{
    private const string TableName = "onetimeauthkeys";
    private readonly ITableService _tableService;

    public OneTimeAuthKeysTableService(ITableServiceFactory tableServiceFactory)
    {
        _tableService = tableServiceFactory.Create(TableName);
    }

    public async Task<bool> AreDetailsValidAsync(string username, string key)
    {
        const int maxResultsPerPage = 1;
        var selectProps = new List<string> { nameof(OneTimeAuthKeyEntity.RowKey) };

        var tableClient = _tableService.GetTableClient();

        var results = tableClient.QueryAsync<OneTimeAuthKeyEntity>($"PartitionKey eq '{username}' and RowKey eq '{key}'",
                                                                   maxResultsPerPage,
                                                                   selectProps,
                                                                   CancellationToken.None);

        await foreach (var _ in results)
        {
            return true;
        }

        return false;
    }

    public async Task DeleteKeyAsync(string partitionKey, string rowKey)
    {
        var tableClient = _tableService.GetTableClient();

        await tableClient.DeleteEntityAsync(partitionKey,
                                            rowKey,
                                            ifMatch: default,
                                            cancellationToken: CancellationToken.None);
    }

    public async Task StoreKeyAsync(OneTimeAuthKeyEntity oneTimeAuthKeyEntity)
    {
        await _tableService.StoreTableEntitiesAsync(oneTimeAuthKeyEntity);
    }
}
