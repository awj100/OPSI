using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage;

public interface ITableService
{
    Lazy<TableClient> TableClient { get; }

    Task DeleteTableEntitiesAsync(IEnumerable<KeyPolicy> keyPolicies);

    Task DeleteTableEntityAsync(string partitionKey, string rowKey);

    Task DeleteTableEntityAsync(KeyPolicy keyPolicy);

    Task ExecuteQueryBatchAsync(IReadOnlyCollection<TableTransactionAction> transactions);

    IReadOnlyCollection<BatchedQueryWrapper> GetStoreTableEntitiesBatch(IEnumerable<KeyPolicy> keyPolicies, ITableEntity tableEntity);

    Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities);

    Task<IReadOnlyList<Response>> StoreTableEntitiesAsync(params ITableEntity[] tableEntities);

    Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(IEnumerable<ITableEntity> tableEntities);

    Task<IReadOnlyList<Response>> UpdateTableEntitiesAsync(params ITableEntity[] tableEntities);
}