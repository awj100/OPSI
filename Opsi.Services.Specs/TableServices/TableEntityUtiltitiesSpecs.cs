using System.Text;
using Azure;
using Azure.Data.Tables;
using FluentAssertions;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class TableEntityUtiltitiesSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReadOnlyCollection<string> _ignorablePropertyNames;
    private TableEntity _tableEntity;
    private TestClass _testInstance;
    private TableEntityUtilities _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _ignorablePropertyNames = typeof(ITableEntity).GetProperties()
                                                      .Select(propInfo => propInfo.Name)
                                                      .ToList();

        _testInstance = new TestClass
        {
            BoolProperty = true,
            ByteArrayProperty = Encoding.UTF8.GetBytes("TEST BYTE ARRAY VALUE"),
            DateTimeProperty = DateTime.Now,
            DateTimeOffsetProperty = DateTime.Now,
            DoubleProperty = 12.34,
            IntProperty = 10,
            LongProperty = long.MaxValue,
            StringProperty = "TEST STRING VALUE",

            PartitionKey = "TEST PARTITION KEY",
            RowKey = "TEST ROW KEY"
        };

        _tableEntity = new TableEntity
        {
            [nameof(TestClass.BoolProperty)] = _testInstance.BoolProperty,
            [nameof(TestClass.ByteArrayProperty)] = _testInstance.ByteArrayProperty,
            [nameof(TestClass.DateTimeProperty)] = _testInstance.DateTimeProperty,
            [nameof(TestClass.DateTimeOffsetProperty)] = _testInstance.DateTimeOffsetProperty,
            [nameof(TestClass.DoubleProperty)] = _testInstance.DoubleProperty,
            [nameof(TestClass.GuidProperty)] = _testInstance.GuidProperty,
            [nameof(TestClass.IntProperty)] = _testInstance.IntProperty,
            [nameof(TestClass.LongProperty)] = _testInstance.LongProperty,
            [nameof(TestClass.StringProperty)] = _testInstance.StringProperty
        };

        _testee = new TableEntityUtilities();
    }

    [TestMethod]
    public void GetPropertyNames_WhenTypeHasInheritance_ReturnsTypesPropertyNamesFromTypeAndParent()
    {
        var result = _testee.GetPropertyNames<TestDerivedClass>();

        result.Should().Contain(nameof(TestBaseClass.BoolProperty));
        result.Should().Contain(nameof(TestClass.IntProperty));
        result.Should().Contain(nameof(TestClass.StringProperty));

        result.Count.Should().Be(3);
    }

    [TestMethod]
    public void GetPropertyNames_WhenTypeHasNoInheritance_ReturnsTypesPropertyNames()
    {
        var result = _testee.GetPropertyNames<TestClass>();

        result.Should().Contain(nameof(TestClass.BoolProperty));
        result.Should().Contain(nameof(TestClass.ByteArrayProperty));
        result.Should().Contain(nameof(TestClass.DateTimeProperty));
        result.Should().Contain(nameof(TestClass.DateTimeOffsetProperty));
        result.Should().Contain(nameof(TestClass.DoubleProperty));
        result.Should().Contain(nameof(TestClass.GuidProperty));
        result.Should().Contain(nameof(TestClass.IntProperty));
        result.Should().Contain(nameof(TestClass.LongProperty));
        result.Should().Contain(nameof(TestClass.StringProperty));

        result.Should().Contain(nameof(TestClass.ETag));
        result.Should().Contain(nameof(TestClass.PartitionKey));
        result.Should().Contain(nameof(TestClass.RowKey));
        result.Should().Contain(nameof(TestClass.Timestamp));

        result.Count.Should().Be(13);
    }

    [TestMethod]
    public void ParseTableEntityAs_WhenIgnorablePropertyNamesSpecified_ReturnsInstanceWithoutIgnoredPropertyValue()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.PartitionKey.Should()
                   .NotBe(_testInstance.PartitionKey).And
                   .Be(default);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedBoolPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.BoolProperty.Should().Be(_testInstance.BoolProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedByteArrayPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.ByteArrayProperty.Should().BeEquivalentTo(_testInstance.ByteArrayProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedDateTimePropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.DateTimeProperty.Should().Be(_testInstance.DateTimeProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedDateTimeOffsetPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.DateTimeOffsetProperty.Should().Be(_testInstance.DateTimeOffsetProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedDoublePropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.DoubleProperty.Should().Be(_testInstance.DoubleProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedGuidPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);
        
        result.GuidProperty.Should().Be(_testInstance.GuidProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedIntPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.IntProperty.Should().Be(_testInstance.IntProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedLongPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.LongProperty.Should().Be(_testInstance.LongProperty);
    }

    [TestMethod]
    public void ParseTableEntityAs_ReturnsInstanceWithExpectedStringPropertyValues()
    {
        var result = _testee.ParseTableEntityAs<TestClass>(_tableEntity, _ignorablePropertyNames);

        result.StringProperty.Should().Be(_testInstance.StringProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_WhenIgnorablePropertyNamesSpecified_ReturnsInstanceWithoutIgnoredPropertyValue()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().PartitionKey.Should().Be(default);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedBoolPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().BoolProperty.Should().Be(_testInstance.BoolProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedByteArrayPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().ByteArrayProperty.Should().BeEquivalentTo(_testInstance.ByteArrayProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedDateTimePropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().DateTimeProperty.Should().Be(_testInstance.DateTimeProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedDateTimeOffsetPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().DateTimeOffsetProperty.Should().Be(_testInstance.DateTimeOffsetProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedDoublePropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().DoubleProperty.Should().Be(_testInstance.DoubleProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedGuidPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().GuidProperty.Should().Be(_testInstance.GuidProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedIntPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().IntProperty.Should().Be(_testInstance.IntProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedLongPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().LongProperty.Should().Be(_testInstance.LongProperty);
    }

    [TestMethod]
    public void ParseTableEntityAsType_ReturnsInstanceWithExpectedStringPropertyValues()
    {
        var typeForActivation = typeof(TestClass);

        var result = _testee.ParseTableEntityAsType(typeForActivation, _tableEntity, _ignorablePropertyNames);

        result.Should().BeOfType<TestClass>();
        result.As<TestClass>().StringProperty.Should().Be(_testInstance.StringProperty);
    }

    private class TestClass : ITableEntity
    {
        public bool BoolProperty { get; set; }

        public byte[]? ByteArrayProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }

        public DateTimeOffset DateTimeOffsetProperty { get; set; }

        public double DoubleProperty { get; set; }

        public Guid GuidProperty { get; set; }

        public int IntProperty { get; set; }

        public long LongProperty { get; set; }

        public string? StringProperty { get; set; }

        public ETag ETag { get; set; }
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
    }

    private class TestDerivedClass : TestBaseClass
    {
        public int IntProperty { get; set; }

        public string? StringProperty { get; set; }
    }

    private abstract class TestBaseClass
    {
        public bool BoolProperty { get; set; }
    }
}
