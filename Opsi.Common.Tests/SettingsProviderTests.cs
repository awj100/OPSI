using FluentAssertions;

namespace Opsi.Common.Tests;

[TestClass]
public class SettingsProviderTests
{
    private const string configuredIntVariableName = nameof(configuredIntVariableName);
    private const int configuredIntVariableValue = 9876;
    private const string configuredStringVariableName = nameof(configuredStringVariableName);
    private const string configuredStringVariableValue = nameof(configuredStringVariableValue);
    private const string unrecognisedVariableName = nameof(unrecognisedVariableName);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private SettingsProvider _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _testee = new SettingsProvider();
        Environment.SetEnvironmentVariable(configuredStringVariableName, configuredStringVariableValue, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(configuredIntVariableName, configuredIntVariableValue.ToString(), EnvironmentVariableTarget.Process);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Environment.SetEnvironmentVariable(configuredStringVariableName, null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(configuredIntVariableName, null, EnvironmentVariableTarget.Process);
    }

    [TestMethod]
    public void GetValueAsString_WhenVariableNotConfiguredAndNullUnacceptable_ThrowsMeaningfulException()
    {
        const bool isNullAcceptable = false;
        const string callerName = nameof(GetValueAsString_WhenVariableNotConfiguredAndNullUnacceptable_ThrowsMeaningfulException);

        _testee.Invoking(y => y.GetValue(unrecognisedVariableName, isNullAcceptable, callerName))
            .Should()
            .Throw<Exception>()
            .WithMessage($"*{unrecognisedVariableName}*");
    }

    [TestMethod]
    public void GetValueAsString_WhenVariableNotConfiguredAndNullIsAcceptable_ReturnsNull()
    {
        const bool isNullAcceptable = true;
        const string callerName = nameof(GetValueAsString_WhenVariableNotConfiguredAndNullIsAcceptable_ReturnsNull);

        _testee.GetValue(unrecognisedVariableName, isNullAcceptable, callerName)
            .Should()
            .BeNull();
    }

    [TestMethod]
    public void GetValueAsString_WhenVariableIsConfigured_ReturnsExpectedValue ()
    {
        const bool isNullAcceptable = false;
        const string callerName = nameof(GetValueAsString_WhenVariableIsConfigured_ReturnsExpectedValue);

        _testee.GetValue(configuredStringVariableName, isNullAcceptable, callerName)
            .Should()
            .Be(configuredStringVariableValue);
    }

    [TestMethod]
    public void GetValueAsInt_WhenVariableNotConfiguredAndNullUnacceptable_ThrowsException()
    {
        const bool isNullAcceptable = false;
        const string callerName = nameof(GetValueAsInt_WhenVariableNotConfiguredAndNullUnacceptable_ThrowsException);

        _testee.Invoking(y => y.GetValue<int>(unrecognisedVariableName, isNullAcceptable, callerName))
            .Should()
            .Throw<Exception>();
    }

    [TestMethod]
    public void GetValueAsInt_WhenVariableNotConfiguredAndNullIsAcceptable_Returns0()
    {
        const bool isNullAcceptable = true;
        const string callerName = nameof(GetValueAsInt_WhenVariableNotConfiguredAndNullIsAcceptable_Returns0);

        _testee.GetValue<int>(unrecognisedVariableName, isNullAcceptable, callerName)
            .Should()
            .Be(default);
    }

    [TestMethod]
    public void GetValueAsInt_WhenVariableIsConfigured_ReturnsExpectdValue()
    {
        const bool isNullAcceptable = false;
        const string callerName = nameof(GetValueAsInt_WhenVariableIsConfigured_ReturnsExpectdValue);

        _testee.GetValue<int>(configuredIntVariableName, isNullAcceptable, callerName)
            .Should()
            .Be(configuredIntVariableValue);
    }
}
