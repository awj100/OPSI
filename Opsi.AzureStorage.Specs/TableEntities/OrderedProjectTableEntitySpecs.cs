using System.Reflection;
using FluentAssertions;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.Specs;

[TestClass]
public class OrderedProjectTableEntitySpecs
{
    private const string PartitionKey = "PARTITION KEY";
    private const string RowKey = "ROW KEY";
    private const string _name = "TEST PROJECT NAME";
    private Guid _id;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Func<OrderedProject, IReadOnlyCollection<KeyPolicy>> _keyPolicyResolvers;
    private OrderedProject _orderedProject;
    private OrderedProjectTableEntity _orderedProjectTableEntity;
    private Project _project;
    private ProjectTableEntity _projectTableEntity;
    private IReadOnlyCollection<string> _rowKeys;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _id = Guid.NewGuid();

        var i = 0;
        _rowKeys = new List<string>
        {
            $"{RowKey} {i++}",
            $"{RowKey} {i++}"
        };
        _keyPolicyResolvers = orderedProject => _rowKeys.Select(rowKey => new KeyPolicy(PartitionKey, new RowKey(rowKey, KeyPolicyQueryOperators.Equal))).ToList();

        _orderedProject = new OrderedProject
        {
            Id = _id,
            Name = _name
        };

        _orderedProjectTableEntity = new OrderedProjectTableEntity
        {
            Id = _id,
            Name = _name
        };

        _project = new Project
        {
            Id = _id,
            Name = _name
        };

        _projectTableEntity = new ProjectTableEntity
        {
            Id = _id,
            Name = _name,
            PartitionKey = PartitionKey,
            RowKey = RowKey
        };
    }

    [TestMethod]
    public void FromOrderedProject_WhenKeysAreExplicitlySpecified_ReturnsObjectWithExpectedValues()
    {
        var orderedProjectTableEntity = OrderedProjectTableEntity.FromOrderedProject(_orderedProject, PartitionKey, RowKey);

        foreach (var propInfo in _orderedProject.GetType()
                                                .GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var assignedPropertyValue = propInfo.GetValue(_orderedProject);
            var originalPropertyValue = propInfo.GetValue(orderedProjectTableEntity);

            assignedPropertyValue.Should().Be(originalPropertyValue);
        }

        orderedProjectTableEntity.PartitionKey.Should().Be(PartitionKey);
        orderedProjectTableEntity.RowKey.Should().Be(RowKey);
    }

    [TestMethod]
    public void FromOrderedProject_WhenKeysAreObtainedThroughResolverFunction_ReturnsObjectsWithExpectedValues()
    {
        var orderedProjectTableEntities = OrderedProjectTableEntity.FromOrderedProject(_orderedProject, _keyPolicyResolvers);

        orderedProjectTableEntities.Count.Should().Be(_rowKeys.Count);

        foreach (var propInfo in _orderedProject.GetType()
                                                .GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var orderedProjectTableEntity in orderedProjectTableEntities)
            {
                var assignedPropertyValue = propInfo.GetValue(_orderedProject);
                var originalPropertyValue = propInfo.GetValue(orderedProjectTableEntity);

                assignedPropertyValue.Should().Be(originalPropertyValue);
            }
        }

        foreach (var rowKey in _rowKeys)
        {
            var expectedPartitionKey = PartitionKey;
            var expectedRowKey = rowKey;

            orderedProjectTableEntities.Should().Contain(opte => opte.PartitionKey.Equals(expectedPartitionKey)
                                                                 && opte.RowKey.Equals(expectedRowKey));
        }
    }

    [TestMethod]
    public void ToOrderedProject_ReturnsObjectWithExpectedValues()
    {
        var orderedProject = _orderedProjectTableEntity.ToOrderedProject();
        
        foreach (var propInfo in orderedProject.GetType()
                                               .GetProperties(BindingFlags.Instance|BindingFlags.Public))
        {
            var assignedPropertyValue = propInfo.GetValue(orderedProject);
            var originalPropertyValue = propInfo.GetValue(_orderedProjectTableEntity);

            assignedPropertyValue.Should().Be(originalPropertyValue);
        }
    }

    [TestMethod]
    public void FromProject_ReturnsObjectWithExpectedValues()
    {
        var orderedProjectTableEntity = OrderedProjectTableEntity.FromProject(_project, PartitionKey, RowKey);

        orderedProjectTableEntity.Id.Should().Be(_project.Id);
        orderedProjectTableEntity.Name.Should().Be(_project.Name);
        orderedProjectTableEntity.PartitionKey.Should().Be(PartitionKey);
        orderedProjectTableEntity.RowKey.Should().Be(RowKey);
    }

    [TestMethod]
    public void FromProjectTableEntity_ReturnsObjectWithExpectedValues()
    {
        var orderedProjectTableEntity = OrderedProjectTableEntity.FromProjectTableEntity(_projectTableEntity);

        orderedProjectTableEntity.Id.Should().Be(_projectTableEntity.Id);
        orderedProjectTableEntity.Name.Should().Be(_projectTableEntity.Name);
        orderedProjectTableEntity.PartitionKey.Should().Be(PartitionKey);
        orderedProjectTableEntity.RowKey.Should().Be(RowKey);
    }
}
