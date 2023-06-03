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

        await tableClient.CreateIfNotExistsAsync();

        await tableClient.AddEntityAsync<ITableEntity>(tableEntity);
    }
}
