using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Functions.Functions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Specs.Functions;

[TestClass]
public class ProjectHandlerSpecs
{
    private readonly Guid _projectId = Guid.NewGuid();
    private const string _projectName = "TEST PROJECT NAME";
    private const string _projectState = "TEST PROJECT STATE";
    private const string _username = "user@test.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ProjectWithResources _projectWithResources;
    private IReadOnlyCollection<Resource> _resources;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResponseSerialiser _responseSerialiser;
    private ProjectHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        ProjectWithResources? nullResponse = null;

        _resources = GenerateResources().Take(2).ToList();

        _projectWithResources = new ProjectWithResources
        {
            Id = _projectId,
            Name = _projectName,
            Resources = _resources,
            State = _projectState,
            Username = _username
        };

        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _responseSerialiser = A.Fake<IResponseSerialiser>();

        A.CallTo(() => _projectsService.GetProjectAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(_projectWithResources);
        A.CallTo(() => _projectsService.GetProjectAsync(A<Guid>.That.Not.Matches(g => g.Equals(_projectId)))).Returns(nullResponse);

        _testee = new ProjectHandler(_projectsService,
                                     _errorQueueService,
                                     _loggerFactory,
                                     _responseSerialiser);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognised_ReturnsOk()
    {
        var url = $"/projects/{_projectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _projectId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognised_UsesResponseSerialisedToWriteContentInResponse()
    {
        var url = $"/projects/{_projectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _projectId);

        A.CallTo(() => _responseSerialiser.WriteJsonToBody(A<HttpResponseData>._, A<ProjectWithResources>.That.Matches(p => p.Id.Equals(_projectId)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsNotRecognised_ReturnsBadRequest()
    {
        var unrecognisedProjectId = Guid.NewGuid();
        var url = $"/projects/{unrecognisedProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, unrecognisedProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private IEnumerable<ResourceTableEntity> GenerateResources()
    {
        var i = 0;

        while (true)
        {
            i++;

            yield return new ResourceTableEntity
            {
                FullName = $"TEST RESOURCE {i}",
                ProjectId = _projectId,
                Username = _username,
                VersionId = $"VERSION ID {i}",
                VersionIndex = i
            };
        }
    }
}
