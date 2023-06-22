using FluentAssertions;
using Opsi.Services.Auth;

namespace Opsi.Services.Specs.Auth;

[TestClass]
public class ReferenceAuthHandlerSpecs
{
    private const string AuthScheme = "Basic";
    private const string Username = "user@test.com";
    private readonly IReadOnlyCollection<string> Claims = new List<string> { "Claim1", "Administrator" };
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private AuthHandlerBase _authHandler;
    private IDictionary<object, object> _contextItems;
    private string _encodedAuthParameter;
    private string _unencodedAuthParameter;
    private ReferenceAuthHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _authHandler = new DummyAuthHandler();
        _contextItems = new Dictionary<object, object>();
        _unencodedAuthParameter = $"{Username}:{String.Join(",", Claims)}";
        _encodedAuthParameter = $"{AuthScheme} {EncodeBase64String(_unencodedAuthParameter)}";

        _testee = new ReferenceAuthHandler();
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsObtained_PopulatesContextItemsWithClaims()
    {
        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().Contain(kvp => (string)kvp.Key == _authHandler.ItemNameClaims);
        var resultClaims = _contextItems[_authHandler.ItemNameClaims] as IReadOnlyCollection<string>;
        resultClaims.Should().NotBeNullOrEmpty();
        resultClaims.Should().Contain(Claims);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsObtained_PopulatesContextItemsWithIsAdministrator()
    {
        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().Contain(kvp => (string)kvp.Key == _authHandler.ItemNameIsAdministrator);
        _contextItems[_authHandler.ItemNameIsAdministrator].Should().Be(true);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsObtained_PopulatesContextItemsWithUsername()
    {
        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().Contain(kvp => (string)kvp.Key == _authHandler.ItemNameUsername);
        _contextItems[_authHandler.ItemNameUsername].Should().Be(Username);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenDetailsObtained_ReturnsTrue()
    {
        var result = await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenNoClaimsObtained_DoesNotPopulatesContextItemsWithClaims()
    {
        _unencodedAuthParameter = Username;
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameClaims);

        // ---

        _unencodedAuthParameter = $"{Username}:";
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameClaims);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenNoClaimsObtained_DoesNotPopulatesContextItemsWithIsAdministrator()
    {
        _unencodedAuthParameter = Username;
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameIsAdministrator);

        // ---

        _unencodedAuthParameter = $"{Username}:";
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameIsAdministrator);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenNoUsernameObtained_DoesNotPopulatesContextItemsWithUsername()
    {
        _unencodedAuthParameter = $"{String.Join(",", Claims)}";
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        _contextItems.Should().NotContain(kvp => (string)kvp.Key == _authHandler.ItemNameUsername);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenNoClaimsObtained_ReturnsFalse()
    {
        _unencodedAuthParameter = Username;
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        var result = await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        result.Should().BeFalse();

        // ---

        _unencodedAuthParameter = $"{Username}:";
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        result = await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenNoUsernameObtained_ReturnsFalse()
    {
        _unencodedAuthParameter = $"{String.Join(",", Claims)}";
        _encodedAuthParameter = EncodeBase64String(_unencodedAuthParameter);

        var result = await _testee.AuthenticateAsync(_encodedAuthParameter, _contextItems);

        result.Should().BeFalse();
    }

    private static string EncodeBase64String(string stringToEncode)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(stringToEncode);
        return Convert.ToBase64String(bytes);
    }

    private class DummyAuthHandler : AuthHandlerBase
    { }
}
