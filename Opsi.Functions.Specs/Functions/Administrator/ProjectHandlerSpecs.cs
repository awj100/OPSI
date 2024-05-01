using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common.Exceptions;
using Opsi.Functions.Functions.Administrator;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Specs.Functions.Administrator;

[TestClass]
public class ProjectHandlerSpecs
{
    private readonly Guid _invalidProjectId = Guid.NewGuid();
    private const string _projectName = "TEST PROJECT NAME";
    private const string _projectState = "TEST PROJECT STATE";
    private const string _username = "user@test.com";
    private readonly Guid _validProjectId = Guid.NewGuid();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ProjectWithResources _projectWithResources;
    private IReadOnlyCollection<Resource> _resources;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResponseSerialiser _responseSerialiser;
    private IUserProvider _userProvider;
    private ProjectHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _resources = GenerateResources().Take(2).ToList();

        _projectWithResources = new ProjectWithResources
        {
            Id = _validProjectId,
            Name = _projectName,
            Resources = _resources,
            State = _projectState,
            Username = _username
        };

        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _responseSerialiser = A.Fake<IResponseSerialiser>();
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _projectsService.GetProjectAsync(A<Guid>.That.Matches(g => g.Equals(_validProjectId)))).Returns(_projectWithResources);
        A.CallTo(() => _projectsService.GetProjectAsync(A<Guid>.That.Matches(g => g.Equals(_invalidProjectId)))).ThrowsAsync(new ProjectNotFoundException());
        A.CallTo(() => _userProvider.Username).Returns(_username);

        _testee = new ProjectHandler(_projectsService,
                                     _userProvider,
                                     _errorQueueService,
                                     _loggerFactory,
                                     _responseSerialiser);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognised_ReturnsOkWithProject()
    {
        var url = $"/_admin/projects/{_validProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _validProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognised_UsesResponseSerialisedToWriteContentInResponse()
    {
        var url = $"/_admin/projects/{_validProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _validProjectId);

        A.CallTo(() => _responseSerialiser.WriteJsonToBody(A<HttpResponseData>._, A<ProjectWithResources>.That.Matches(p => p.Id.Equals(_validProjectId)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsNotRecognised_ReturnsBadRequest()
    {
        var url = $"/_admin/projects/{_invalidProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _invalidProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private IEnumerable<Resource> GenerateResources()
    {
        var i = 0;

        while (true)
        {
            i++;

            yield return new Resource
            {
                FullName = $"TEST RESOURCE {i}",
                ProjectId = _validProjectId,
                CreatedBy = _username
            };
        }
    }
}
