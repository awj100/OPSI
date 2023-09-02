using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage.Specs;

[TestClass]
public class BatchedQueryWrapperUtilitiesSpecs
{
    private const string PartitionKey1 = "PARTITION KEY 1";
    private const string PartitionKey2 = "PARTITION KEY 2";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private List<TableTransactionAction> _actionsList1;
    private List<TableTransactionAction> _actionsList2;
    private IEnumerable<BatchedQueryWrapper> _batchedQueryWrappers1;
    private IEnumerable<BatchedQueryWrapper> _batchedQueryWrappers2;
    private ITableEntity _entity1;
    private ITableEntity _entity2;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _entity1 = A.Fake<ITableEntity>();
        _entity2 = A.Fake<ITableEntity>();

        _actionsList1 = new List<TableTransactionAction> {
            new TableTransactionAction(TableTransactionActionType.Add, _entity1)
        };
        _actionsList2 = new List<TableTransactionAction> {
            new TableTransactionAction(TableTransactionActionType.Delete, _entity2)
        };

        _batchedQueryWrappers1 = new List<BatchedQueryWrapper> {
            new BatchedQueryWrapper(PartitionKey1, _actionsList1),
            new BatchedQueryWrapper(PartitionKey2, _actionsList1)
        };
        _batchedQueryWrappers2 = new List<BatchedQueryWrapper> {
            new BatchedQueryWrapper(PartitionKey1, _actionsList2),
            new BatchedQueryWrapper(PartitionKey2, _actionsList2)
        };
    }

    [TestMethod]
    public void CombineByPartitionKey_WhenFirstIsEmpty_ReturnsAllFromSecond()
    {
        var first = new List<BatchedQueryWrapper>(0);
        var second = _batchedQueryWrappers2;

        var result = BatchedQueryWrapperUtilities.CombineByPartitionKey(first, second);

        result.Should().NotBeNullOrEmpty();
        result.Count.Should().Be(_batchedQueryWrappers2.Count());
        result.Single().PartitionKey.Should().Be(_batchedQueryWrappers2.Single().PartitionKey);
        result.Single().Actions.Count.Should().Be(_batchedQueryWrappers2.Single().Actions.Count);
        result.Single().Actions.Single().Should().Be(_batchedQueryWrappers2.Single().Actions.Single());
    }

    [TestMethod]
    public void CombineByPartitionKey_WhenSecondIsEmpty_ReturnsAllFromFirst()
    {
        var first = _batchedQueryWrappers1;
        var second = new List<BatchedQueryWrapper>(0);

        var result = BatchedQueryWrapperUtilities.CombineByPartitionKey(first, second);

        result.Should().NotBeNullOrEmpty();
        result.Count.Should().Be(_batchedQueryWrappers2.Count());
        result.Single().PartitionKey.Should().Be(_batchedQueryWrappers1.Single().PartitionKey);
        result.Single().Actions.Count.Should().Be(_batchedQueryWrappers1.Single().Actions.Count);
        result.Single().Actions.Single().Should().Be(_batchedQueryWrappers1.Single().Actions.Single());
    }

    [TestMethod]
    public void CombineByPartitionKey_WhenFirstAndSecondAreNotEmpty_ReturnsAllFromFirstAndSecond()
    {
        var first = _batchedQueryWrappers1;
        var second = _batchedQueryWrappers2;

        var result = BatchedQueryWrapperUtilities.CombineByPartitionKey(first, second);

        result.Should().NotBeNullOrEmpty();
        result.Count.Should().Be(2);
        result.Select(bqw => bqw.PartitionKey).Should().Contain(_batchedQueryWrappers1.Select(bqwInner => bqwInner.PartitionKey));
        result.Select(bqw => bqw.PartitionKey).Should().Contain(_batchedQueryWrappers2.Select(bqwInner => bqwInner.PartitionKey));

        result.Count(bqw => bqw.PartitionKey.Equals(PartitionKey1)).Should().Be(1);
        result.Count(bqw => bqw.PartitionKey.Equals(PartitionKey2)).Should().Be(1);

        result.Single(bqw => bqw.PartitionKey.Equals(PartitionKey1)).Actions.Count().Should().Be(2);
        result.Single(bqw => bqw.PartitionKey.Equals(PartitionKey2)).Actions.Count().Should().Be(2);
    }
}
