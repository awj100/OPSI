using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Common;

namespace Opsi.AzureStorage;

internal class TableService : StorageServiceBase, ITableService
{
    private readonly IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;

    public Lazy<TableClient> TableClient { get; }

    public string TableName { get; }

    public TableService(ISettingsProvider settingsProvider,
                        string tableName,
                        IKeyPolicyFilterGeneration keyPolicyFilterGeneration) : base(settingsProvider)
    {
        _keyPolicyFilterGeneration = keyPolicyFilterGeneration;
        TableName = tableName;

        TableClient = new Lazy<TableClient>(() =>
        {
            var tableServiceClient = new TableServiceClient(StorageConnectionString.Value);
            return tableServiceClient.GetTableClient(TableName);
        });
    }

    public async Task DeleteTableEntitiesAsync(IEnumerable<KeyPolicy> keyPolicies)
    {
        // This is based on the maximum number of conditions permitted in a table query filter.
        // The maximum query conditions is 15 but we will format a string using one common partition key
        // and up to 14 row keys. Consequently we cannot consider more than 14 row keys, therefore we
        // cannot permit more than 14 KeyPolicy instances.
        const int maxKeyPolicies = 14;

        if (!keyPolicies.Any())
        {
            return;
        }

        if (keyPolicies.Any(keyPolicy => keyPolicy.RowKey.QueryOperator != KeyPolicyQueryOperators.Equal))
        {
            throw new InvalidOperationException($"{nameof(KeyPolicy)}.{nameof(KeyPolicy.RowKey)}.{nameof(KeyPolicy.RowKey.QueryOperator)} must be \"{KeyPolicyQueryOperators.Equal}\" when calling {nameof(DeleteTableEntityAsync)}({nameof(KeyPolicy)}).");
        }

        if (keyPolicies.Count() > maxKeyPolicies)
        {
            throw new InvalidOperationException($"No more than {maxKeyPolicies} {nameof(KeyPolicy)} instances may be specified.");
        }

        foreach (var keyPolicy in keyPolicies)
        {
            await TableClient.Value.DeleteEntityAsync(keyPolicy.PartitionKey, keyPolicy.RowKey.Value);
        }
    }

    public async Task DeleteTableEntityAsync(string partitionKey, string rowKey)
    {
        await TableClient.Value.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task DeleteTableEntityAsync(KeyPolicy keyPolicy)
    {
        if (keyPolicy.RowKey.QueryOperator != KeyPolicyQueryOperators.Equal)
        {
            throw new InvalidOperationException($"{nameof(KeyPolicy)}.{nameof(KeyPolicy.RowKey)}.{nameof(KeyPolicy.RowKey.QueryOperator)} must be \"{KeyPolicyQueryOperators.Equal}\" when calling {nameof(DeleteTableEntityAsync)}({nameof(KeyPolicy)}).");
        }

        await TableClient.Value.DeleteEntityAsync(keyPolicy.PartitionKey, keyPolicy.RowKey.Value);
    }

    public async Task ExecuteQueryBatchAsync(IReadOnlyCollection<TableTransactionAction> transactions)
    {
        await TableClient.Value.SubmitTransactionAsync(transactions);
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

        try
        {
            var batch = tableEntities.Select(entity => new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity))
                                     .ToList();

            var batchResult = await TableClient.Value.SubmitTransactionAsync(batch);

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

        try
        {
            var batch = tableEntities.Select(entity => new TableTransactionAction(TableTransactionActionType.UpdateMerge, entity))
                                     .ToList();

            var batchResult = await TableClient.Value.SubmitTransactionAsync(batch);

            return batchResult.Value;
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to update {tableEntities.First().GetType().Name}: {exception.Message}");
        }
    }
}
