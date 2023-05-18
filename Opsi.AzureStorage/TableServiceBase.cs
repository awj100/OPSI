using Azure.Data.Tables;
using Opsi.Common;

namespace Opsi.AzureStorage;

public abstract class TableServiceBase : StorageServiceBase
{
    private readonly string _tableName;

    public TableServiceBase(ISettingsProvider settingsProvider, string tableName) : base(settingsProvider)
    {
        _tableName = tableName;
    }

    protected virtual async Task DeleteTableEntityAsync(string partitionKey, string rowKey)
    {
        var tableClient = GetTableClient();

        await tableClient.CreateIfNotExistsAsync();

        await tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    protected virtual TableClient GetTableClient()
    {
        var tableServiceClient = new TableServiceClient(StorageConnectionString.Value);
        return tableServiceClient.GetTableClient(_tableName);
    }

    protected virtual async Task StoreTableEntityAsync(ITableEntity tableEntity)
    {
        var tableClient = GetTableClient();

        await tableClient.CreateIfNotExistsAsync();

        await tableClient.AddEntityAsync<ITableEntity>(tableEntity);
    }
}
