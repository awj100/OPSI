using FluentAssertions;
using Opsi.Constants;

namespace Opsi.Common.Tests;

[TestClass]
public class ProjectStatesExtensionsTests
{
    [TestMethod]
    public void GetValidProjectState_WhenPossibleProjectStateIsInvalid_ReturnsOptionIsNone()
    {
        const string invalidProjectState = "INVALID PROJECT STATE";

        var result = ProjectStatesExtensions.GetValidProjectState(invalidProjectState);

        result.IsNone.Should().BeTrue();
    }

    [TestMethod]
    public void GetValidProjectState_WhenPossibleProjectStateIsValidAndLowerCase_ReturnsOptionIsSome()
    {
        var validProjectState = ProjectStates.Completed.ToLower();

        var result = ProjectStatesExtensions.GetValidProjectState(validProjectState);

        result.IsSome.Should().BeTrue();
    }

    [TestMethod]
    public void GetValidProjectState_WhenPossibleProjectStateIsValidAndMatchingCase_ReturnsOptionIsSome()
    {
        var validProjectState = ProjectStates.Completed;

        var result = ProjectStatesExtensions.GetValidProjectState(validProjectState);

        result.IsSome.Should().BeTrue();
    }
}
