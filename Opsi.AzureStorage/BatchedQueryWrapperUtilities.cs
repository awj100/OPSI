using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

public static class BatchedQueryWrapperUtilities
{
    public static IReadOnlyCollection<BatchedQueryWrapper> CombineByPartitionKey(this IEnumerable<BatchedQueryWrapper> first, IEnumerable<BatchedQueryWrapper> second)
    {
        if (first == null)
        {
            throw new ArgumentException($"Cannot be null.", nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentException($"Cannot be null.", nameof(second));
        }

        if (!first.Any())
        {
            return second.ToList();
        }

        if (second == null || !second.Any())
        {
            return first.ToList();
        }

        var list = new List<BatchedQueryWrapper>();

        foreach (var batchedQueryWrapper in first)
        {
            var partitionKeyFirst = batchedQueryWrapper.PartitionKey;

            var others = second.Where(bqw => bqw.PartitionKey.Equals(partitionKeyFirst))
                               .SelectMany(bqw => bqw.Actions)
                               .ToList();

            var groupedActions = batchedQueryWrapper.Actions.Union(others).ToList();

            list.Add(new BatchedQueryWrapper(partitionKeyFirst, groupedActions));
        }

        return list;
    }
}
