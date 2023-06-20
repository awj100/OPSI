using FakeItEasy;
using FluentAssertions;
using Opsi.Services.Auth.OneTimeAuth;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.Auth.OneTimeAuth;

[TestClass]
public class OneTimeAuthServiceSpecs
{
    private const string AuthScheme = "OneTime";
    private const string Username = "user@test.com";
    private const string FilePath = "file/path";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _key;
    private Guid _projectId;
    private IOneTimeAuthKeyProvider _oneTimeAuthKeyProvider;
    private IOneTimeAuthKeysTableService _tableService;
    private string _uniqueKey;
    private OneTimeAuthService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _projectId = Guid.NewGuid();
        _uniqueKey = Guid.NewGuid().ToString();
        _key = $"{_uniqueKey}_{_projectId}_{FilePath}";

        _oneTimeAuthKeyProvider = A.Fake<IOneTimeAuthKeyProvider>();
        _tableService = A.Fake<IOneTimeAuthKeysTableService>();

        A.CallTo(() => _oneTimeAuthKeyProvider.GenerateUniqueKey()).Returns(_uniqueKey);
        A.CallTo(() => _tableService.AreDetailsValidAsync(A<string>.That.Matches(s => s.Equals(Username)),
                                                          A<string>.That.Matches(s => s.Equals(_key))))
            .Returns(true);

        _testee = new OneTimeAuthService(_oneTimeAuthKeyProvider, _tableService);
    }

    [TestMethod]
    public async Task GeneratedAuthHeaderIsSuccessfullyDecoded()
    {
        var authHeader = await _testee.GetAuthenticationHeaderAsync(Username, _projectId, FilePath);

        authHeader.Should().NotBeNull();

        var username = await _testee.GetUsernameAsync(authHeader.ToString());

        username.Should().Be(Username);
    }

    [TestMethod]
    public async Task GetUsernameAsync_WhenKeyIsValid_DeletesKeyFromStorage()
    {
        var authHeader = await _testee.GetAuthenticationHeaderAsync(Username, _projectId, FilePath);

        await _testee.GetUsernameAsync(authHeader.ToString());

        A.CallTo(() => _tableService.DeleteKeyAsync(A<string>.That.Matches(s => s.Equals(Username)),
                                                    A<string>.That.Matches(s => s.Equals(_key))))
            .MustHaveHappenedOnceExactly();
    }
}
