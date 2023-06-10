using System.Net;
using FakeItEasy;
using FluentAssertions;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.Specs.Http;

namespace Opsi.Services.Specs.QueueHandlers.Dependencies;

[TestClass]
public class ResourceDispatcherSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _filePath = "file/path";
    private string _hostUrl = "https://request.not.sent";
    private IHttpClientFactory _httpClientFactory;
    private Guid _projectId = Guid.NewGuid();
    private Stream _testStream;
    private Uri _testUri;
    private ResourceDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _httpClientFactory = A.Fake<IHttpClientFactory>();
        _testStream = new MemoryStream();
        _testUri = new Uri($"{_hostUrl}/projects/{_projectId}/resource/{_filePath}");

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

        var response = await _testee.DispatchAsync(_hostUrl,
                                                   _projectId,
                                                   _filePath,
                                                   _testStream);

        response.StatusCode.Should().Be(httpStatusCode);
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
    }
}
