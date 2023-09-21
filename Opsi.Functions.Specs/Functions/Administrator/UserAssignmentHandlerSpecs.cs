using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common;
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
    private const string _assignedByUsernameInvalid = "INVALID ASSIGNED BY USERNAME";
    private const string _assignedByUsernameValid = "VALID ASSIGNED BY USERNAME";
    private const string _assigneeUsername = "TEST ASSIGNEE USERNAME";
    private const string _resourceFullName = "TEST RESOURCE FULL NAME";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Guid _projectId;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IUserProvider _userProvider;
    private string _uri;
    private UserAssignment _userAssignment;
    private UserAssignmentHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _projectId = Guid.NewGuid();
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _uri = "/assignments";
        _userProvider = A.Fake<IUserProvider>();
        _userAssignment = new UserAssignment
        {
            AssignedByUsername = _assignedByUsernameInvalid,
            AssigneeUsername = _assigneeUsername,
            ProjectId = _projectId,
            ResourceFullName = _resourceFullName
        };

        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _assignedByUsernameValid));

        _testee = new UserAssignmentHandler(_projectsService,
                                            _userProvider,
                                            _errorQueueService,
                                            _loggerFactory);
    }

    [TestMethod]
    public async Task Run_WhenNoUserAssignmentIsInBody_ReturnsBadRequest()
    {
        var request = TestFactory.CreateHttpRequest(_uri);
        var response = await _testee.Run(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Run_WhenUserAssignmentIsInBody_PassesUserAssignmentWithAssignedByUsernameFromUserProvider()
    {
        var request = await TestFactory.CreateHttpRequestAsync(_uri, _userAssignment);
        var response = await _testee.Run(request);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedByUsername.Equals(_assignedByUsernameValid))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenUserAssignmentIsInBody_PassesUserAssignmentWithPopulatedAssignedOnUtc()
    {
        DateTime lowerBound = DateTime.UtcNow;

        var request = await TestFactory.CreateHttpRequestAsync(_uri, _userAssignment);
        var response = await _testee.Run(request);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssignedOnUtc >= lowerBound
                                                                                             && ua.AssignedOnUtc <= DateTime.UtcNow)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenUserAssignmentIsInBody_PassesUserAssignmentWithAssigneeUsernameFromBodyContent()
    {
        var request = await TestFactory.CreateHttpRequestAsync(_uri, _userAssignment);
        var response = await _testee.Run(request);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.AssigneeUsername.Equals(_userAssignment.AssigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenUserAssignmentIsInBody_PassesUserAssignmentWithProjectIdFromBodyContent()
    {
        var request = await TestFactory.CreateHttpRequestAsync(_uri, _userAssignment);
        var response = await _testee.Run(request);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectId.Equals(_userAssignment.ProjectId))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenUserAssignmentIsInBody_PassesUserAssignmentWithResourceFullNameFromBodyContent()
    {
        var request = await TestFactory.CreateHttpRequestAsync(_uri, _userAssignment);
        var response = await _testee.Run(request);

        A.CallTo(() => _projectsService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ResourceFullName.Equals(_userAssignment.ResourceFullName))))
         .MustHaveHappenedOnceExactly();
    }
}