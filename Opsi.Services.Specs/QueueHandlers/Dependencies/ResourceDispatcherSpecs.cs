using System.Net;
using System.Net.Http.Headers;
using FakeItEasy;
using FluentAssertions;
using Opsi.Services.Auth.OneTimeAuth;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.Specs.Http;

namespace Opsi.Services.Specs.QueueHandlers.Dependencies;

[TestClass]
public class ResourceDispatcherSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private const string AuthHeaderScheme = "TestScheme";
    private const string AuthHeaderParameter = "Test Parameter";
    private const string FilePath = "file/path";
    private const string HostUrl = "https://request.not.sent";
    private IHttpClientFactory _httpClientFactory;
    private IOneTimeAuthService _oneTimeAuthService;
    private readonly Guid _projectId = Guid.NewGuid();
    private Stream _testStream;
    private Uri _testUri;
    private const string Username = "user@test.com";
    private ResourceDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _httpClientFactory = A.Fake<IHttpClientFactory>();
        _oneTimeAuthService = A.Fake<IOneTimeAuthService>();
        _testStream = new MemoryStream();
        _testUri = new Uri($"{HostUrl}/projects/{_projectId}/resource/{FilePath}");

        A.CallTo(() => _oneTimeAuthService.GetAuthenticationHeaderAsync(Username, _projectId, FilePath))
         .Returns(Task.FromResult(new AuthenticationHeaderValue(AuthHeaderScheme, AuthHeaderParameter)));

        _testee = new ResourceDispatcher(_httpClientFactory, _oneTimeAuthService);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _testStream?.Dispose();
    }

    [TestMethod]
    public async Task DispatchAsync_ReturnsResponseFromHttpClient()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_testUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var response = await _testee.DispatchAsync(HostUrl,
                                                   _projectId,
                                                   FilePath,
                                                   _testStream,
                                                   Username);

        response.StatusCode.Should().Be(httpStatusCode);
    }

    [TestMethod]
    public async Task DispatchAsync_GetsOneTimeAuthKeyFromService()
    {
        var response = await _testee.DispatchAsync(HostUrl,
                                                   _projectId,
                                                   FilePath,
                                                   _testStream,
                                                   Username);

        A.CallTo(() => _oneTimeAuthService.GetAuthenticationHeaderAsync(Username, _projectId, FilePath))
         .MustHaveHappenedOnceExactly();
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
    }
}
