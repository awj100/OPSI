using FluentAssertions;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services.Specs;

[TestClass]
public class ErrorTableEntitySpecs
{
    private const string InnerError = nameof(InnerError);
    private const string Message = nameof(Message);
    private const string Origin = nameof(Origin);

    [TestMethod]
    public void ctor_PopulatesPartitionKeyWithExpectedValue()
    {
        var error = GetPopulatedError();

        var errorEntity = new ErrorTableEntity(error);

        errorEntity.PartitionKey
            .Should()
            .Be(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [TestMethod]
    public void ctor_PopulatesRowKeyWithGuid()
    {
        var error = GetPopulatedError();

        var errorEntity = new ErrorTableEntity(error);

        errorEntity.RowKey
            .Should()
            .NotBeNull();

        Guid.TryParse(errorEntity.RowKey, out var _).Should().BeTrue();
    }

    [TestMethod]
    public void ctor_WhenSettingRowKeyWithNonGuidFormattedString_ThrowsArgumentException()
    {
        var error = GetPopulatedError();

        var errorEntity = new ErrorTableEntity(error);

        try
        {
            errorEntity.RowKey = "a plain string";
        }
        catch(ArgumentException)
        {
            return;
        }

        Assert.Fail("RowKey accepted a string which wasn't formatted as a GUID.");
    }

    [TestMethod]
    public void ctor_WhenSettingRowKeyWithGuidFormattedString_ThrowsNoException()
    {
        var newRowKey = Guid.NewGuid().ToString();

        var error = GetPopulatedError();

        var errorEntity = new ErrorTableEntity(error) { RowKey = newRowKey };

        errorEntity.RowKey.Should().Be(newRowKey);
    }

    private static Error GetPopulatedError()
    {
        var error = new Error();
        var errorType = error.GetType();

        foreach (var propInfo in errorType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                          .Where(propInfo => propInfo.PropertyType == typeof(string)))
        {
            propInfo.SetValue(error, $"{propInfo.Name}_value");
        }

        return error;
    }
}
