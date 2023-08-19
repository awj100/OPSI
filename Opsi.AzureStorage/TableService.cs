using Azure;
using Azure.Data.Tables;
using Opsi.Common;

namespace Opsi.AzureStorage;

internal class TableService : StorageServiceBase, ITableService
{
    public string TableName { get; }

    public TableService(ISettingsProvider settingsProvider, string tableName) : base(settingsProvider)
    {
        TableName = tableName;
    }

    public async Task DeleteTableEntityAsync(string partitionKey, string rowKey)
    {
        var tableClient = GetTableClient();

        await tableClient.CreateIfNotExistsAsync();

        await tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public TableClient GetTableClient()
    {
        var tableServiceClient = new TableServiceClient(StorageConnectionString.Value);
        return tableServiceClient.GetTableClient(TableName);
    }

    public async Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities)
    {
        return await StoreTableEntitiesAsync(tableEntities.ToArray());
    }

    public async Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(params ITableEntity[] tableEntities)
    {
        if (!tableEntities.Any())
        {
            return Enumerable.Empty<Response>().ToList();
        }

        var tableClient = GetTableClient();

        try
        {
            var batch = tableEntities.Select(entity => new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity))
                                     .ToList();

            var batchResult = await tableClient.SubmitTransactionAsync(batch);

            return batchResult.Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return await UpdateTableEntitiesAsync(tableEntities);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to store {tableEntities.First().GetType().Name}: {exception.Message}");
        }
    }

    public async Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities)
    {
        return await UpdateTableEntitiesAsync(tableEntities.ToArray());
    }

    public async Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(params ITableEntity[] tableEntities)
    {
        if (!tableEntities.Any())
        {
            return Enumerable.Empty<Response>().ToList();
        }

        var tableClient = GetTableClient();

        try
        {
            var batch = tableEntities.Select(entity => new TableTransactionAction(TableTransactionActionType.UpdateMerge, entity))
                                     .ToList();

            var batchResult = await tableClient.SubmitTransactionAsync(batch);

            return batchResult.Value;
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to update {tableEntities.First().GetType().Name}: {exception.Message}");
        }
    }
}
