using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Functions.Functions.Administrator;
using Opsi.Functions.Specs;
using Opsi.Services;
using Opsi.Services.QueueServices;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Opsi.Functions.Functions.Specs.Administrator;

[TestClass]
public class ProjectStateHandlerSpecs
{
    private const string _newProjectState = "InProgress";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Guid _projectId;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private string _uri;
    private ProjectStateHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _projectId = Guid.NewGuid();
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _uri = $"/projects/{_projectId}/{_newProjectState}";

        _testee = new ProjectStateHandler(_projectsService,
                                          _errorQueueService,
                                          _loggerFactory);
    }

    [TestMethod]
    public async Task Run_WhenStateIsInvalid_PassesProjectIdAndStateToProjectsService()
    {
        const string invalidState = "INVALID STATE";
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, _projectId, invalidState);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_PassesProjectIdAndStateToProjectsService()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request, _projectId, _newProjectState);

        A.CallTo(() => _projectsService.UpdateProjectStateAsync(_projectId, _newProjectState)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.UpdateProjectStateAsync(_projectId, _newProjectState)).ThrowsAsync(new ArgumentException());

        var response = await _testee.Run(request, _projectId, _newProjectState);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenProjectServiceThrowsException_ReturnsInternalServerError()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        A.CallTo(() => _projectsService.UpdateProjectStateAsync(_projectId, _newProjectState)).ThrowsAsync(new Exception());

        var response = await _testee.Run(request, _projectId, _newProjectState);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}