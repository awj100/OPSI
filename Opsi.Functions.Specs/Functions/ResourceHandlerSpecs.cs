using System.Net;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Functions.Functions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Specs.Functions;

[TestClass]
public class ResourceHandlerSpecs
{
    private Guid _projectId = Guid.NewGuid();
    private static Random _random = new Random();
    private const string _restOfPath = "folder/filename.ext";
    private const string _username = "user@test.com";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IResourceService _resourceService;
    private IUserProvider _userProvider;
    private ResourceHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _resourceService = A.Fake<IResourceService>();
        _userProvider = A.Fake<IUserProvider>();

        _testee = new ResourceHandler(_resourceService,
                                              _errorQueueService,
                                              _userProvider,
                                              _loggerFactory);
    }

    [TestMethod]
    public async Task Run_WhenUserHasNoAccess_ReturnsForbidden()
    {
        A.CallTo(() => _resourceService.GetResourceContentAsync(_projectId, A<string>._)).ThrowsAsync(new UnassignedToResourceException());
        var url = GetUrl();
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _projectId, _restOfPath);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Run_WhenNoResourceFound_ReturnsNotFoundWithExplanation()
    {
        A.CallTo(() => _resourceService.HasUserAccessAsync(A<Guid>._, A<string>._)).Returns(true);
        A.CallTo(() => _resourceService.GetResourceContentAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)), A<string>.That.Matches(s => s.Equals(_restOfPath, StringComparison.OrdinalIgnoreCase)))).Returns(Option<ResourceContent>.None());
        var url = GetUrl();
        var request = TestFactory.CreateHttpRequest(url, HttpMethod.Get);

        var response = await _testee.Run(request, _projectId, _restOfPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var s = response.Body;
        s.Position = 0;
        using var ms = new MemoryStream((int)s.Length);
        await s.CopyToAsync(ms);
        var bodyContentAsText = Encoding.UTF8.GetString(ms.ToArray());
        bodyContentAsText.Should().Contain(_restOfPath);
    }

    [TestMethod]
    public async Task Run_WhenResourceFound_ReturnsOkWithExpectedContent()
    {
        var blobContentLength = _random.Next(500);
        var blobContents = GenerateRandomString(blobContentLength);
        const string blobFullName = "folder/filename.ext";
        var etag = new Azure.ETag("0x1C038C9CCB1AA00").ToString("H");

        var resourceContent = new ResourceContent(blobFullName,
                                                  Encoding.UTF8.GetBytes(blobContents.ToArray()),
                                                  blobContentLength,
                                                  "text/plain",
                                                  DateTimeOffset.UtcNow,
                                                  etag);

        A.CallTo(() => _resourceService.HasUserAccessAsync(A<Guid>._, A<string>._)).Returns(true);
        A.CallTo(() => _resourceService.GetResourceContentAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)), A<string>.That.Matches(s => s.Equals(_restOfPath, StringComparison.OrdinalIgnoreCase)))).Returns(Option<ResourceContent>.Some(resourceContent));
        var url = GetUrl();
        var request = TestFactory.CreateHttpRequest(url, HttpMethod.Get);

        var response = await _testee.Run(request, _projectId, _restOfPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var s = response.Body;
        s.Position = 0;
        using var ms = new MemoryStream((int)s.Length);
        await s.CopyToAsync(ms);
        var bodyContentAsText = Encoding.UTF8.GetString(ms.ToArray());
        bodyContentAsText.Should().Contain(blobContents);
    }

    [TestMethod]
    public async Task Run_WhenFetchingResourceThrowsException_ExceptionPlacedInErrorQueue()
    {
        var ex = new Exception(nameof(Run_WhenFetchingResourceThrowsException_ExceptionPlacedInErrorQueue));
        A.CallTo(() => _resourceService.GetResourceContentAsync(A<Guid>._, A<string>._)).ThrowsAsync(ex);
        var url = GetUrl();
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _projectId, _restOfPath);

        A.CallTo(() => _errorQueueService.ReportAsync(A<Exception>.That.Matches(ex2 => ex.Message == ex2.Message), A<LogLevel>._, A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenFetchingResourceThrowsException_ReturnsInternalServerError()
    {
        var ex = new Exception(nameof(Run_WhenFetchingResourceThrowsException_ReturnsInternalServerError));
        A.CallTo(() => _resourceService.GetResourceContentAsync(A<Guid>._, A<string>._)).ThrowsAsync(ex);
        var url = GetUrl();
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _projectId, _restOfPath);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new String(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private string GetUrl()
    {
        return $"/projects/{_projectId}/resources/{_restOfPath}";
    }
}
