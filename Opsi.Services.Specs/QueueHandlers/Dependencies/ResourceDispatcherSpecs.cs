using System.Net;
using System.Net.Http.Headers;
using FakeItEasy;
using FluentAssertions;
using Opsi.Constants;
using Opsi.Services.Auth.OneTimeAuth;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.Specs.Http;

namespace Opsi.Services.Specs.QueueHandlers.Dependencies;

[TestClass]
public class ResourceDispatcherSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private const string FilePath = "file/path";
    private const string HostUrl = "https://request.not.sent";
    private IHttpClientFactory _httpClientFactory;
    private readonly Guid _projectId = Guid.NewGuid();
    private Stream _testStream;
    private string _testContent;
    private Uri _testUri;
    private const string Username = "user@test.com";
    private ResourceDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _testContent = Guid.NewGuid().ToString();

        var testContentBytes = System.Text.Encoding.UTF8.GetBytes(_testContent);
        _testStream = new MemoryStream(testContentBytes);

        _httpClientFactory = A.Fake<IHttpClientFactory>();
        _testUri = new Uri($"{HostUrl}/projects/{_projectId}/resource/{FilePath}");

        _testee = new ResourceDispatcher(_httpClientFactory);
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
    public async Task DispatchAsync_SendsSpecifiedStream()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new ContentConditionalUriAndResponse(_testUri,
                                                                  responseMessage,
                                                                  async content => {
                                                                      if (content is MultipartFormDataContent multipartFormDataContent)
                                                                      {
                                                                          multipartFormDataContent.Count().Should().Be(1);

                                                                          if (multipartFormDataContent.Single() is StreamContent streamContent)
                                                                          {
                                                                              var s = await streamContent.ReadAsStringAsync();

                                                                              s.Should().Be(_testContent);
                                                                          }

                                                                          return true;
                                                                      }

                                                                      return false;
                                                                  });

        ConfigureHttpResponse(uriAndResponse);

        var response = await _testee.DispatchAsync(HostUrl,
                                                   _projectId,
                                                   FilePath,
                                                   _testStream,
                                                   Username);

        response.StatusCode.Should().Be(httpStatusCode);
    }

    [TestMethod]
    public async Task DispatchAsync_UsesOneTimeAuthClient()
    {
        var response = await _testee.DispatchAsync(HostUrl,
                                                   _projectId,
                                                   FilePath,
                                                   _testStream,
                                                   Username);

        A.CallTo(() => _httpClientFactory.CreateClient(HttpClientNames.OneTimeAuth)).MustHaveHappenedOnceExactly();
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
    }
}
