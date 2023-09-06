using System.Net;
using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Functions.Functions.Administrator;
using Opsi.Functions.Specs;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Specs.Administrator;

[TestClass]
public class ProjectsHandlerSpecs
{
    private const int _defaultPageSize = 50;
    private const string _projectState = "InProgress";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _defaultOrderBy;
    private string _invalidOrderBy;
    private string? _nullOrderBy = null;
    private string _validOrderBy;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResponseSerialiser _responseSerialiser;
    private string _uri;
    private IUserProvider _userProvider;
    private ProjectsHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _defaultOrderBy = OrderBy.Asc;
        _invalidOrderBy = "INVALID ORDER BY";
        _validOrderBy = OrderBy.Desc;
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _responseSerialiser = new ResponseSerialiser();
        _userProvider = A.Fake<IUserProvider>();
        _uri = $"/projects/{_projectState}";

        _testee = new ProjectsHandler(_projectsService,
                                      _responseSerialiser,
                                      _errorQueueService,
                                      _userProvider,
                                      _loggerFactory);
    }

    [TestMethod]
    public async Task Run_PassesProjectStateToProjectsService()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, _projectState, _validOrderBy, null, null);

        A.CallTo(() => _projectsService.GetProjectsAsync(_projectState, A<string>._, A<int>._, A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenInvalidOrderBySpecified_ReturnsBadRequest()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, String.Empty, _invalidOrderBy, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenNoOrderBySpecified_PassesDefaultOrderByToProjectsService()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, String.Empty, _nullOrderBy, null);

        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>.That.Matches(orderBy => orderBy.Equals(_defaultOrderBy)), A<int>._, A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenNoPageSizeSpecified_PassesDefaultPageSizeToProjectsService()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, String.Empty, null, null);

        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>.That.Matches(pageSize => pageSize.Equals(_defaultPageSize)), A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenPageSizeSpecified_PassesSpecifiedPageSizeToProjectsService()
    {
        const int specifiedPageSize = 5;

        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, String.Empty, _validOrderBy, specifiedPageSize, null);

        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>.That.Matches(pageSize => pageSize.Equals(specifiedPageSize)), A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenContinuationTokenSpecified_PassesSpecifiedContinuationTokenToProjectsService()
    {
        var continuationToken = Guid.NewGuid().ToString();

        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, String.Empty, _validOrderBy, null, continuationToken);

        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>._, continuationToken)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>._, A<string?>._)).ThrowsAsync(new ArgumentException());

        var response = await _testee.Run(request, _projectState, _validOrderBy, null, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenProjectServiceThrowsException_ReturnsInternalServerError()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>._, A<string?>._)).ThrowsAsync(new Exception());

        var response = await _testee.Run(request, _projectState, _validOrderBy, null, null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task Run_ReturnsOk()
    {
        var continuationToken = Guid.NewGuid().ToString();
        var projects = GenerateProjects().Take(2).ToList();
        var pageableProjectsResponse = new PageableResponse<Project>(projects, continuationToken);

        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.GetProjectsAsync(_projectState, _validOrderBy, _defaultPageSize, null)).Returns(pageableProjectsResponse);

        var response = await _testee.Run(request, _projectState, _validOrderBy, null, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Run_ReturnsPageableResponseWithExpectedContinuationToken()
    {
        var continuationToken = Guid.NewGuid().ToString();
        var projects = GenerateProjects().Take(2).ToList();
        var pageableProjectsResponse = new PageableResponse<Project>(projects, continuationToken);

        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>._, A<string?>._)).Returns(pageableProjectsResponse);

        var response = await _testee.Run(request, _projectState, _validOrderBy, _defaultPageSize, null);

        var responsePageableResponse = await ParseBodyAsAsync<PageableResponse<Project>>(response.Body);
        responsePageableResponse.Should().NotBeNull();
        responsePageableResponse.ContinuationToken.Should().Be(continuationToken);
    }

    [TestMethod]
    public async Task Run_ReturnsPageableResponseWithExpectedProjects()
    {
        var continuationToken = Guid.NewGuid().ToString();
        var projects = GenerateProjects().Take(2).ToList();
        var pageableProjectsResponse = new PageableResponse<Project>(projects, continuationToken);

        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.GetProjectsAsync(A<string>._, A<string>._, A<int>._, A<string?>._)).Returns(pageableProjectsResponse);

        var response = await _testee.Run(request, _projectState, _validOrderBy, null, null);

        var responsePageableResponse = await ParseBodyAsAsync<PageableResponse<Project>>(response.Body);
        responsePageableResponse.Items.Should().NotBeNullOrEmpty();
        responsePageableResponse.Items.Should().HaveCount(projects.Count);
        foreach (var project in projects)
        {
            responsePageableResponse!.Items.SingleOrDefault(responseProject => responseProject.Id.Equals(project.Id)).Should().NotBeNull();
        }
    }

    private static IEnumerable<Project> GenerateProjects()
    {
        var i = 0;

        while (true)
        {
            yield return new Project
            {
                Id = Guid.NewGuid(),
                Name = $"Project_{i++}",
                State = _projectState,
                Username = "user@test.com"
            };
        }
    }

    private static async Task<T?> ParseBodyAsAsync<T>(Stream responseBody)
    {
        var _serialisationOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        return await JsonSerializer.DeserializeAsync<T>(responseBody, _serialisationOptions);
    }
}