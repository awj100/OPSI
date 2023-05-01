using Azure.Data.Tables;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.AzureStorage;

public abstract class TableServiceBase
{
    private readonly string _storageConnectionString;
    private readonly string _tableName;

    public TableServiceBase(string storageConnectionString, string tableName)
    {
        _storageConnectionString = storageConnectionString;
        _tableName = tableName;
    }

    protected virtual TableClient GetTableClient()
    {
        var tableServiceClient = new TableServiceClient(_storageConnectionString);
        return tableServiceClient.GetTableClient(_tableName);
    }

    protected virtual async Task StoreTableEntityAsync(ITableEntity tableEntity)
    {
        var tableClient = GetTableClient();

        await tableClient.CreateIfNotExistsAsync();

        await tableClient.AddEntityAsync<ITableEntity>(tableEntity);
    }
}
