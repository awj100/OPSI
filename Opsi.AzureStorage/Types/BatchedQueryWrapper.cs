using Azure.Data.Tables;

namespace Opsi.AzureStorage.Types;

public readonly struct BatchedQueryWrapper
{
    public string PartitionKey { get; }

    public IReadOnlyCollection<TableTransactionAction> Actions { get; }

    public BatchedQueryWrapper(string partitionKey, IReadOnlyCollection<TableTransactionAction> actions)
    {
        Actions = actions;
        PartitionKey = partitionKey;
    }

    public override string ToString()
    {
        var actionGroups = String.Join(", ", Actions.GroupBy(action => action.ActionType)
                                                    .Select(actionGroup => $"{actionGroup.Key} ({actionGroup.Count()})"));

        return $"{PartitionKey} | {actionGroups}";
    }
}
