using Azure;
using Azure.Data.Tables;

namespace Opsi.AzureStorage;

public interface ITableService
{
    Task DeleteTableEntityAsync(string partitionKey, string rowKey);

    TableClient GetTableClient();

    Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities);

    Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(params ITableEntity[] tableEntities);

    Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities);

    Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(params ITableEntity[] tableEntities);
}