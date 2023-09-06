using FluentAssertions;
using Opsi.Constants;

namespace Opsi.Common.Tests;

[TestClass]
public class OrderByExtensionsTests
{
    [TestMethod]
    public void GetValidOrderBy_WhenPossibleOrderByIsInvalid_ReturnsOptionIsNone()
    {
        const string invalidOrderBy = "INVALID ORDERBY";

        var result = OrderByExtensions.GetValidOrderBy(invalidOrderBy);

        result.IsNone.Should().BeTrue();
    }

    [TestMethod]
    public void GetValidOrderBy_WhenPossibleOrderByIsValidAndLowerCase_ReturnsOptionIsSome()
    {
        var validOrderBy = OrderBy.Desc.ToLower();

        var result = OrderByExtensions.GetValidOrderBy(validOrderBy);

        result.IsSome.Should().BeTrue();
    }

    [TestMethod]
    public void GetValidOrderBy_WhenPossibleOrderByIsValidAndMatchingCase_ReturnsOptionIsSome()
    {
        var validOrderBy = OrderBy.Desc;

        var result = OrderByExtensions.GetValidOrderBy(validOrderBy);

        result.IsSome.Should().BeTrue();
    }
}
