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

    public async Task StoreTableEntityAsync(ITableEntity tableEntity)
    {
        var tableClient = GetTableClient();

        try
        {
            await tableClient.AddEntityAsync(tableEntity);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            await UpdateTableEntityAsync(tableEntity);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to store {tableEntity.GetType().Name}: {exception.Message}");
        }
    }

    public async Task UpdateTableEntityAsync(ITableEntity tableEntity)
    {
        var tableClient = GetTableClient();

        try
        {
            await tableClient.UpdateEntityAsync(tableEntity, ETag.All, TableUpdateMode.Replace);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to update {tableEntity.GetType().Name}: {exception.Message}");
        }
    }
}
