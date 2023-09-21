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
public class AssignedProjectsHandlerSpecs
{
    private const string _url = "/projects";
    private const string _unnasignedUsername = "unassigned_user@test.com";
    private const string _username = "user@test.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReadOnlyCollection<UserAssignment> _userAssignments;
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResponseSerialiser _responseSerialiser;
    private IUserProvider _userProvider;
    private AssignedProjectsHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        var emptyResponse = new List<UserAssignment>(0);

        _userAssignments = GenerateUserAssignments().Take(2).ToList();

        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _responseSerialiser = A.Fake<IResponseSerialiser>();
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _projectsService.GetAssignedProjectsAsync(A<string>.That.Matches(s => s.Equals(_username)))).Returns(_userAssignments);
        A.CallTo(() => _projectsService.GetAssignedProjectsAsync(A<string>.That.Not.Matches(s => s.Equals(_username)))).Returns(emptyResponse);
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _username));

        _testee = new AssignedProjectsHandler(_projectsService,
                                              _responseSerialiser,
                                              _errorQueueService,
                                              _userProvider,
                                              _loggerFactory);
    }

    [TestMethod]
    public async Task Run_WhenNoProjectsAreAssigned_ReturnsOkWithEmptyList()
    {
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _unnasignedUsername));
        var request = TestFactory.CreateHttpRequest(_url);

        var response = await _testee.Run(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        A.CallTo(() => _responseSerialiser.WriteJsonToBody(A<HttpResponseData>._, A<IReadOnlyCollection<UserAssignment>>.That.Matches(uas => !uas.Any()))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Run_WhenProjectsAreAssigned_UsesResponseSerialisedToWriteContentInResponse()
    {
        var request = TestFactory.CreateHttpRequest(_url);

        var response = await _testee.Run(request);

        A.CallTo(() => _responseSerialiser.WriteJsonToBody(A<HttpResponseData>._, A<IReadOnlyCollection<UserAssignment>>.That.Matches(uas => uas.Count.Equals(_userAssignments.Count)))).MustHaveHappenedOnceExactly();
    }

    private static IEnumerable<UserAssignment> GenerateUserAssignments()
    {
        var i = 0;

        while (true)
        {
            i++;

            yield return new UserAssignment
            {
                AssignedByUsername = $"ASSIGNED BY USERNAME {i}",
                AssignedOnUtc = DateTime.UtcNow.AddDays(-1),
                AssigneeUsername = $"ASSIGNEE USERNAME {i}",
                ProjectId = Guid.NewGuid(),
                ProjectName = $"PROJECT NAME {i}",
                ResourceFullName = $"RESOURCE FULL NAME {i}"
            };
        }
    }
}
