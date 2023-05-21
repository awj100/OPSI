using FakeItEasy;
using Opsi.Common;
using Opsi.Notifications.Abstractions;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services.Specs;

[TestClass]
public class ErrorEmailServiceSpecs
{
    private const string ConfigValueSubject = nameof(ErrorEmailService.ConfigNameSubject);
    private const string ConfigValueToAddress = nameof(ErrorEmailService.ConfigNameToAddress);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IEmailNotificationService _emailNotificationService;
    private ISettingsProvider _settingsProvider;
    private ErrorEmailService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _emailNotificationService = A.Fake<IEmailNotificationService>();

        _settingsProvider = A.Fake<ISettingsProvider>();
        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s == ErrorEmailService.ConfigNameSubject),
                                                  A<bool>._,
                                                  A<string>._)).Returns(ConfigValueSubject);
        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s == ErrorEmailService.ConfigNameToAddress),
                                                  A<bool>._,
                                                  A<string>._)).Returns(ConfigValueToAddress);

        _testee = new ErrorEmailService(_emailNotificationService, _settingsProvider);
    }

    [TestMethod]
    public async Task SendAsync_WhenAllErrorPropertiesAreNull_SendsNoEmail()
    {
        var error = new Error();

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>._,
                                                           A<string>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task SendAsync_WhenAllErrorPropertiesAreEmpty_SendsNoEmail()
    {
        var error = new Error
        {
            Message = String.Empty,
            Origin = String.Empty,
            StackTrace = String.Empty
        };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>._,
                                                           A<string>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task SendAsync_UsesConfigValueSubject()
    {
        var error = new Error { Origin = "_" };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>.That.Matches(s => s == ConfigValueSubject),
                                                           A<string>._,
                                                           A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SendAsync_UsesConfigValueToAddress()
    {
        var error = new Error { Origin = "_" };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>._,
                                                           A<string>.That.Matches(s => s == ConfigValueToAddress))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SendAsync_ConfigValueSubjectIsRequired()
    {
        const bool canValueBeNull = false;

        var error = new Error { Origin = "_" };

        await _testee.SendAsync(error);

        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s == ErrorEmailService.ConfigNameSubject),
                                                  canValueBeNull,
                                                  A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SendAsync_ConfigValueToAddressIsRequired()
    {
        const bool canValueBeNull = false;

        var error = new Error { Origin = "_" };

        await _testee.SendAsync(error);

        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s == ErrorEmailService.ConfigNameToAddress),
                                                  canValueBeNull,
                                                  A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SendAsync_SendsEmailWithSpecifiedInnerError()
    {
        const string innerMessage = nameof(innerMessage);
        const string innerOrigin = nameof(innerOrigin);
        const string innerStackTrace = nameof(innerStackTrace);

        var error = new Error
        {
            InnerError = new Error
            {
                Message = innerMessage,
                Origin = innerOrigin,
                StackTrace = innerStackTrace
            }
        };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>.That.Matches(s => s.Contains(innerMessage) && s.Contains(innerOrigin) && s.Contains(innerStackTrace)),
                                                           A<string>._)).MustHaveHappened();
    }

    [TestMethod]
    public async Task SendAsync_SendsEmailWithSpecifiedMessage()
    {
        const string message = nameof(message);

        var error = new Error { Message = message };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>.That.Contains(message),
                                                           A<string>._)).MustHaveHappened();
    }

    [TestMethod]
    public async Task SendAsync_SendsEmailWithSpecifiedOrigin()
    {
        const string origin = nameof(origin);

        var error = new Error { Origin = origin };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>.That.Contains(origin),
                                                           A<string>._)).MustHaveHappened();
    }

    [TestMethod]
    public async Task SendAsync_SendsEmailWithSpecifiedStackTrace()
    {
        const string stackTrace = nameof(stackTrace);

        var error = new Error { StackTrace = stackTrace };

        await _testee.SendAsync(error);

        A.CallTo(() => _emailNotificationService.SendAsync(A<string>._,
                                                           A<string>.That.Contains(stackTrace),
                                                           A<string>._)).MustHaveHappened();
    }
}
