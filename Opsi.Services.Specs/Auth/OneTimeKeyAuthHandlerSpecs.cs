using FakeItEasy;
using FluentAssertions;
using Opsi.Services.Auth;
using Opsi.Services.Auth.OneTimeAuth;

namespace Opsi.Services.Specs.Auth;

[TestClass]
public class OneTimeKeyAuthHandlerSpecs
{
    private const bool IsAdministrator = true;
    private const string Username = "user@test.com";
    private readonly OneTimeAuthCredentials _credentials = new(Username, IsAdministrator, true);
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private AuthHandlerBase _authHandler;
    private IDictionary<object, object> _contextItems;
    private string _authenticationHeader;
    private IOneTimeAuthService _oneTimeAuthService;
    private OneTimeKeyAuthHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _authenticationHeader = Guid.NewGuid().ToString();
        _authHandler = new DummyAuthHandler();
        _contextItems = new Dictionary<object, object>();

        _oneTimeAuthService = A.Fake<IOneTimeAuthService>();

        _testee = new OneTimeKeyAuthHandler(_oneTimeAuthService);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsAreValid_PopulatesContextItemsWithUsername()
    {
        A.CallTo(() => _oneTimeAuthService.GetCredentialsAsync(_authenticationHeader)).Returns(_credentials);

        await _testee.AuthenticateAsync(_authenticationHeader, _contextItems);

        _contextItems.Should().Contain(kvp => (string)kvp.Key == _authHandler.ItemNameUsername);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsAreValid_ReturnsTrue()
    {
        A.CallTo(() => _oneTimeAuthService.GetCredentialsAsync(_authenticationHeader)).Returns(_credentials);

        var result = await _testee.AuthenticateAsync(_authenticationHeader, _contextItems);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsAreInvalid_PopulatesContextItemsWithUsername()
    {
        await _testee.AuthenticateAsync(_authenticationHeader, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameUsername);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsAreInvalid_ReturnsFalse()
    {
        var result = await _testee.AuthenticateAsync(_authenticationHeader, _contextItems);

        result.Should().BeFalse();
    }

    private class DummyAuthHandler : AuthHandlerBase
    { }
}
