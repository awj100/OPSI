using System.Net;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common.Exceptions;
using Opsi.Functions.Functions.Administrator;
using Opsi.Functions.Specs;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Opsi.Functions.Functions.Specs.Administrator;

[TestClass]
public class UserAssignmentHandlerSpecs
{
    private const string _assignedByUsernameValid = "VALID ASSIGNED BY USERNAME";
    private const string _assigneeUsername = "TEST ASSIGNEE USERNAME";
    private const string _resourceFullName = "TEST RESOURCE FULL NAME";
    private readonly HttpMethod _methodDelete = HttpMethod.Delete;
    private readonly HttpMethod _methodPut = HttpMethod.Put;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Guid _projectId;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IUserProvider _userProvider;
    private string _uri;
    private UserAssignmentHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _projectId = Guid.NewGuid();
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _uri = $"/users/{_assigneeUsername}/projects/{_projectId}/resource/{_resourceFullName}";
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _userProvider.Username).Returns(_assignedByUsernameValid);

        _testee = new UserAssignmentHandler(_projectsService,
                                            _userProvider,
                                            _errorQueueService,
                                            _loggerFactory);
    }

    [TestMethod]
    public async Task Run_WhenMethodIsDelete_PassesUserAssignmentWithAssignedByUsernameFromUserProvider()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodDelete);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedByUsername.Equals(_assignedByUsernameValid))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsDelete_PassesUserAssignmentWithPopulatedAssignedOnUtc()
    {
        DateTime lowerBound = DateTime.UtcNow;

        var request = TestFactory.CreateHttpRequest(_uri, _methodDelete);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedOnUtc >= lowerBound
                                                                                             && ua.AssignedOnUtc <= DateTime.UtcNow)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsDelete_PassesUserAssignmentWithAssigneeUsernameFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodDelete);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssigneeUsername.Equals(_assigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsDelete_PassesUserAssignmentWithProjectIdFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodDelete);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectId.Equals(_projectId))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsDelete_PassesUserAssignmentWithResourceFullNameFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodDelete);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.ResourceFullName.Equals(_resourceFullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPut_PassesUserAssignmentWithAssignedByUsernameFromUserProvider()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedByUsername.Equals(_assignedByUsernameValid))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPut_PassesUserAssignmentWithPopulatedAssignedOnUtc()
    {
        DateTime lowerBound = DateTime.UtcNow;

        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedOnUtc >= lowerBound
                                                                                             && ua.AssignedOnUtc <= DateTime.UtcNow)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPut_PassesUserAssignmentWithAssigneeUsernameFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssigneeUsername.Equals(_assigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPut_PassesUserAssignmentWithProjectIdFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectId.Equals(_projectId))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPut_PassesUserAssignmentWithResourceFullNameFromUri()
    {
        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);
        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ResourceFullName.Equals(_resourceFullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenMethodIsPutAndUserAssignmentExceptionIsThrown_ReturnsBadRequestWithExceptionMessage()
    {
        const string exceptionMessage = "The specified resource is already assigned to another user.";

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectId.Equals(_projectId)
                                                                                             && ua.ResourceFullName.Equals(_resourceFullName)
                                                                                             && ua.AssigneeUsername.Equals(_assigneeUsername))))
                                       .ThrowsAsync(new UserAssignmentException(_projectId,
                                                                                _resourceFullName,
                                                                                exceptionMessage));

        var request = TestFactory.CreateHttpRequest(_uri, _methodPut);

        var response = await _testee.Run(request,
                                         _assigneeUsername,
                                         _projectId,
                                         _resourceFullName);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var s = response.Body;
        s.Position = 0;
        using var ms = new MemoryStream((int)s.Length);
        await s.CopyToAsync(ms);
        var bodyContentAsText = Encoding.UTF8.GetString(ms.ToArray());
        bodyContentAsText.Should().Contain(exceptionMessage);
    }
}