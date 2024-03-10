using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common.Exceptions;
using Opsi.Functions.Functions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Specs.Functions;

[TestClass]
public class AssignedProjectHandlerSpecs
{
    private const string _assignedUsername = "user@test.com";
    private readonly Guid _completedProjectId = Guid.NewGuid();
    private readonly Guid _invalidProjectId = Guid.NewGuid();
    private const string _projectName = "TEST PROJECT NAME";
    private const string _projectState = "TEST PROJECT STATE";
    private const string _unassignedUsername = "unassigned_user@test.com";
    private readonly Guid _validProjectId = Guid.NewGuid();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ProjectWithResources _projectWithResources;
    private IReadOnlyCollection<Resource> _resources;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResponseSerialiser _responseSerialiser;
    private IUserProvider _userProvider;
    private AssignedProjectHandler _testee;
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
            Username = _assignedUsername
        };

        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _responseSerialiser = A.Fake<IResponseSerialiser>();
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _projectsService.GetAssignedProjectAsync(A<Guid>.That.Matches(g => g.Equals(_completedProjectId)), A<string>.That.Matches(s => s.Equals(_assignedUsername)))).ThrowsAsync(new ProjectStateException());
        A.CallTo(() => _projectsService.GetAssignedProjectAsync(A<Guid>.That.Matches(g => g.Equals(_validProjectId)), A<string>.That.Matches(s => s.Equals(_assignedUsername)))).Returns(_projectWithResources);
        A.CallTo(() => _projectsService.GetAssignedProjectAsync(A<Guid>.That.Matches(g => g.Equals(_invalidProjectId)), A<string>.That.Matches(s => s.Equals(_assignedUsername)))).ThrowsAsync(new ProjectNotFoundException());
        A.CallTo(() => _projectsService.GetAssignedProjectAsync(A<Guid>.That.Matches(g => g.Equals(_validProjectId)), A<string>.That.Matches(s => s.Equals(_unassignedUsername)))).ThrowsAsync(new UnassignedToProjectException());
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _assignedUsername));

        _testee = new AssignedProjectHandler(_projectsService,
                                             _userProvider,
                                             _errorQueueService,
                                             _loggerFactory,
                                             _responseSerialiser);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognisedAndUserIsAssigned_ReturnsOkWithProject()
    {
        var url = $"/projects/{_validProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _validProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognisedAndUserIsAssigned_UsesResponseSerialisedToWriteContentInResponse()
    {
        var url = $"/projects/{_validProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _validProjectId);

        A.CallTo(() => _responseSerialiser.WriteJsonToBody(A<HttpResponseData>._, A<ProjectWithResources>.That.Matches(p => p.Id.Equals(_validProjectId)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsNotRecognised_ReturnsBadRequest()
    {
        var url = $"/projects/{_invalidProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _invalidProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenProjectIdIsRecognisedAndUserIsAssignedButProjectIsNotInProgress_ReturnsBadRequest()
    {
        var url = $"/projects/{_completedProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        var response = await _testee.Run(request, _completedProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenUserIsNotAssigned_ReturnsUnauthorized()
    {
        var url = $"/projects/{_validProjectId}";
        var request = TestFactory.CreateHttpRequest(url);

        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _unassignedUsername));

        var response = await _testee.Run(request, _validProjectId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
                ProjectId = _validProjectId,
                Username = _assignedUsername
            };
        }
    }
}
