using FakeItEasy;
using FluentAssertions;
using Opsi.Services.Auth;

namespace Opsi.Services.Specs.Auth;

[TestClass]
public class AuthServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IAuthHandler _authHandler1;
    private IAuthHandler _authHandler2;
    private IAuthHandlerProvider _authHandlerProvider;
    private IReadOnlyCollection<IAuthHandler> _authHandlers;
    private string? _authHeader;
    IDictionary<object, object> _contextItems;
    private AuthService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _authHandler1 = A.Fake<IAuthHandler>();
        _authHandler2 = A.Fake<IAuthHandler>();
        _authHandlers = new List<IAuthHandler> {
            _authHandler1,
            _authHandler2
        };
        _authHandlerProvider = A.Fake<IAuthHandlerProvider>();
        _authHeader = Guid.NewGuid().ToString();
        _contextItems = new Dictionary<object, object>();

        A.CallTo(() => _authHandlerProvider.GetAuthHandlers()).Returns(_authHandlers);

        _testee = new AuthService(_authHandlerProvider);
    }

    [TestMethod]
    public async Task TrySetAuthenticationContextItems_WhenFirstAuthHandlerReturnsTrue_ReturnsTrue()
    {
        A.CallTo(() => _authHandler1.AuthenticateAsync(_authHeader!, _contextItems)).Returns(true);
        A.CallTo(() => _authHandler2.AuthenticateAsync(_authHeader!, _contextItems)).Returns(false);

        var result = await _testee.TrySetAuthenticationContextItems(_authHeader, _contextItems);

        result.Should().BeTrue();

        A.CallTo(() => _authHandler1.AuthenticateAsync(_authHeader!, _contextItems)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task TrySetAuthenticationContextItems_WhenFirstAuthHandlerReturnsTrue_DoesNotCallSubsequentAuthHandler()
    {
        A.CallTo(() => _authHandler1.AuthenticateAsync(_authHeader!, _contextItems)).Returns(true);
        A.CallTo(() => _authHandler2.AuthenticateAsync(_authHeader!, _contextItems)).Returns(false);

        var result = await _testee.TrySetAuthenticationContextItems(_authHeader, _contextItems);

        A.CallTo(() => _authHandler2.AuthenticateAsync(_authHeader!, _contextItems)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task TrySetAuthenticationContextItems_WhenFirstAuthHandlerReturnsFalse_CallsSubsequentAuthHandler()
    {
        A.CallTo(() => _authHandler1.AuthenticateAsync(A<string>.That.Matches(s => s.Equals(_authHeader)),
                                                       A<IDictionary<object, object>>._)).Returns(false);
        A.CallTo(() => _authHandler2.AuthenticateAsync(A<string>.That.Matches(s => s.Equals(_authHeader)),
                                                       A<IDictionary<object, object>>._)).Returns(true);

        var result = await _testee.TrySetAuthenticationContextItems(_authHeader, _contextItems);

        result.Should().BeTrue();

        A.CallTo(() => _authHandler1.AuthenticateAsync(A<string>.That.Matches(s => s.Equals(_authHeader)),
                                                       A<IDictionary<object, object>>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _authHandler2.AuthenticateAsync(A<string>.That.Matches(s => s.Equals(_authHeader)),
                                                       A<IDictionary<object, object>>._)).MustHaveHappenedOnceExactly();
    }
}
