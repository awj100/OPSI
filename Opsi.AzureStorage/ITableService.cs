using Azure.Data.Tables;

namespace Opsi.AzureStorage;

public interface ITableService
{
    Task DeleteTableEntityAsync(string partitionKey, string rowKey);

    TableClient GetTableClient();

    Task StoreTableEntityAsync(ITableEntity tableEntity);

    Task UpdateTableEntityAsync(ITableEntity tableEntity);
}