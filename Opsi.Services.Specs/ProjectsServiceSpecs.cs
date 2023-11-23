using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _assignedByUsername = "TEST ASSIGNED BY USERNAME";
    private const string _assigneeUsername1 = "TEST ASSIGNEE USERNAME 1";
    private const string _assigneeUsername2 = "TEST ASSIGNEE USERNAME 2";
    private const string _continuationToken = "TEST CONTINUATION TOKEN";
    private const int _pageSize = 10;
    private const string _projectName = "TEST PROJECT NAME";
    private const string _resource1FullName = "TEST RESOURCE 1 FULL NAME";
    private const string _resource2FullName = "TEST RESOURCE 2 FULL NAME";
    private const string _state2 = "TEST STATE 2";
    private const string _username = "TEST USERNAME";
    private const string _webhookCustomProp1Name = nameof(_webhookCustomProp1Name);
    private const string _webhookCustomProp1Value = nameof(_webhookCustomProp1Value);
    private const string _webhookCustomProp2Name = nameof(_webhookCustomProp2Name);
    private const int _webhookCustomProp2Value = 2;
    private const string _webhookUri = "https://a.test.url";
    private readonly Guid _projectId = Guid.NewGuid();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private DateTime _assignedOnUtc;
    private string _defaultOrderBy = OrderBy.Desc;
    private OrderedProject _orderedProject;
    private Project _project;
    private ProjectTableEntity _projectTableEntity;
    private IProjectsTableService _projectsTableService;
    private readonly string _state1 = ProjectStates.InProgress;
    private ILoggerFactory _loggerFactory;
    private IResourcesService _resourcesService;
    private ResourceTableEntity _resourceTableEntity;
    private UserAssignment _userAssignment1;
    private UserAssignment _userAssignment2;
    private IUserProvider _userProvider;
    private UserAssignmentTableEntity _userAssignmentTableEntity1;
    private UserAssignmentTableEntity _userAssignmentTableEntity2;
    private IWebhookQueueService _webhookQueueService;
    private Dictionary<string, object> _webhookCustomProps;
    private ConsumerWebhookSpecification _webhookSpecs;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _assignedOnUtc = DateTime.UtcNow;

        var basicKeyPolicy = new KeyPolicy("TEST PARTITION KEY", new RowKey("TEST ROW KEY", KeyPolicyQueryOperators.Equal));
        Func<Project, IReadOnlyCollection<KeyPolicy>> basicKeyPolicyResolver = project => new List<KeyPolicy> { basicKeyPolicy };

        _webhookCustomProps = new Dictionary<string, object>
        {
            { _webhookCustomProp1Name, _webhookCustomProp1Value },
            { _webhookCustomProp2Name, _webhookCustomProp2Value },
        };

        _webhookSpecs = new ConsumerWebhookSpecification
        {
            CustomProps = _webhookCustomProps,
            Uri = _webhookUri
        };

        _orderedProject = new OrderedProject
        {
            Id = _projectId,
            Name = _projectName
        };

        _project = new Project
        {
            Id = _projectId,
            Name = _projectName,
            State = _state1,
            Username = _username,
            WebhookSpecification = _webhookSpecs
        };

        _projectTableEntity = new ProjectTableEntity
        {
            EntityType = typeof(ProjectTableEntity).Name,
            EntityVersion = 1,
            Id = _projectId,
            Name = _projectName,
            PartitionKey = "TEST PARTITION KEY",
            RowKey = "TEST ROW KEY",
            State = _state1,
            Username = _username
        };

        _resourceTableEntity = new ResourceTableEntity
        {
            EntityType = typeof(ResourceTableEntity).Name,
            EntityVersion = 1,
            FullName = _resource1FullName,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            RowKey = "TEST ROW KEY",
            Username = _username
        };

        _userAssignmentTableEntity1 = new UserAssignmentTableEntity
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            EntityType = typeof(UserAssignmentTableEntity).Name,
            EntityVersion = 1,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            ProjectName = _projectName,
            RowKey = "TEST ROW KEY",
            ResourceFullName = _resource1FullName
        };
        _userAssignment1 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            ProjectId = _projectId,
            ResourceFullName = _resource1FullName
        };

        _userAssignmentTableEntity2 = new UserAssignmentTableEntity
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername2,
            EntityType = typeof(UserAssignmentTableEntity).Name,
            EntityVersion = 1,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            ProjectName = _projectName,
            RowKey = "TEST ROW KEY",
            ResourceFullName = _resource2FullName
        };
        _userAssignment2 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername2,
            ProjectId = _projectId,
            ResourceFullName = _resource2FullName
        };

        Option<Project> nullProject = Option<Project>.None();

        _loggerFactory = new NullLoggerFactory();
        _projectsTableService = A.Fake<IProjectsTableService>();
        _resourcesService = A.Fake<IResourcesService>();
        _userProvider = A.Fake<IUserProvider>();
        _webhookQueueService = A.Fake<IWebhookQueueService>();

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(_project.Id)).Returns(Option<Project>.Some(_project));
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Not.Matches(g => g.Equals(_project.Id)))).Returns(nullProject);
        A.CallTo(() => _projectsTableService.UpdateProjectStateAsync(_project.Id, _state2)).ReturnsLazily(() =>
        {
            _project.State = _state2;
            return Option<ProjectTableEntity>.Some(ProjectTableEntity.FromProject(_project, basicKeyPolicyResolver).First());
        });
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _username));

        _testee = new ProjectsService(_projectsTableService,
                                      _resourcesService,
                                      _userProvider,
                                      _webhookQueueService,
                                      _loggerFactory);
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenNoProjectWithCorrespondingIdIsFound_ThrowsArgumentException()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.None());

        await _testee.Invoking(t => t.AssignUserAsync(_userAssignment1)).Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_PassesUserAssignmentWithProjectNameFromRetrievedProject()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _projectsTableService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectName.Equals(_project.Name))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectId()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectName()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_project.Name)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectEvent()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserAssigned)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    /*[TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectLevel()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Level.Equals(Levels.)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }*/

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectUsername()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_userAssignment1.AssignedByUsername)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithAssigneeUsernameInCustomProps()
    {
        const string propNameAssignedUsername = "assignedUsername";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameAssignedUsername)
                                                                && cws.CustomProps[propNameAssignedUsername].Equals(_assigneeUsername1))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithResourceFullNameInCustomProps()
    {
        const string propNameResourceFullName = "resourceFullName";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.AssignUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameResourceFullName)
                                                                && cws.CustomProps[propNameResourceFullName].Equals(_resource1FullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoTableEntitiesAreReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>(0);

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoUserAssignmentTableEntityIsReturned_ThrowsUnassignedToProjectException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<UnassignedToProjectException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoProjectTableEntityReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturnedButProjectIsNotInProgress_ThrowsProjectStateException()
    {
        _projectTableEntity.State = ProjectStates.Completed;

        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectStateException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturned_ReturnsProjectWithResources()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNullOrEmpty().And.HaveCount(1);
        result.Resources.Single().FullName.Should().Be(_resource1FullName);
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenNoUserAssignmentsFound_ReturnsEmptyList()
    {
        const string assigneeUsername = "TEST ASSIGNEE USERNAME";
        var userAssignmentTableEntities = new List<UserAssignmentTableEntity>(0);

        A.CallTo(() => _projectsTableService.GetAssignedProjectsAsync(assigneeUsername)).Returns(userAssignmentTableEntities);

        var result = await _testee.GetAssignedProjectsAsync(assigneeUsername);

        result.Should()
            .NotBeNull().And
            .BeEmpty();
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenUserAssignmentsFound_ReturnsMappedUserAssignmentsFromTableService()
    {
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        const string assignedByUsername1 = "TEST ASSIGNED BY USERNAME 1";
        const string assignedByUsername2 = "TEST ASSIGNED BY USERNAME 2";
        const string assigneeUsername = "TEST ASSIGNEE USERNAME";
        const string partitionKey = "TEST PARTITION KEY";
        const string rowKey1 = "TEST ROW KEY 1";
        const string rowKey2 = "TEST ROW KEY 2";
        const string projectName1 = "TEST PROJECT NAME 1";
        const string projectName2 = "TEST PROJECT NAME 2";
        const string resourceFullName1 = "TEST RESOURCE FULL NAME 1";
        const string resourceFullName2 = "TEST RESOURCE FULL NAME 2";

        var userAssignmentTableEntities = new List<UserAssignmentTableEntity>
        {
            UserAssignmentTableEntity.FromUserAssignment(new UserAssignment
                                                         {
                                                             AssignedByUsername = assignedByUsername1,
                                                             AssignedOnUtc = DateTime.UtcNow,
                                                             AssigneeUsername = assigneeUsername,
                                                             ProjectId = projectId1,
                                                             ProjectName = projectName1,
                                                             ResourceFullName = resourceFullName1
                                                         }, new KeyPolicy(partitionKey, new RowKey(rowKey1, KeyPolicyQueryOperators.Equal))),
            UserAssignmentTableEntity.FromUserAssignment(new UserAssignment
                                                         {
                                                             AssignedByUsername = assignedByUsername2,
                                                             AssignedOnUtc = DateTime.UtcNow,
                                                             AssigneeUsername = assigneeUsername,
                                                             ProjectId = projectId2,
                                                             ProjectName = projectName2,
                                                             ResourceFullName = resourceFullName2
                                                         }, new KeyPolicy(partitionKey, new RowKey(rowKey2, KeyPolicyQueryOperators.Equal)))
        };

        A.CallTo(() => _projectsTableService.GetAssignedProjectsAsync(assigneeUsername)).Returns(userAssignmentTableEntities);

        var result = await _testee.GetAssignedProjectsAsync(assigneeUsername);

        result.Should()
            .NotBeNullOrEmpty().And
            .HaveCount(userAssignmentTableEntities.Count);

        foreach (var userAssignmentTableEntity in userAssignmentTableEntities)
        {
            result.Should().Contain(ua => ua.AssignedByUsername.Equals(userAssignmentTableEntity.AssignedByUsername)
                                          && ua.AssignedOnUtc.Equals(userAssignmentTableEntity.AssignedOnUtc)
                                          && ua.AssigneeUsername.Equals(userAssignmentTableEntity.AssigneeUsername)
                                          && ua.ProjectId.Equals(userAssignmentTableEntity.ProjectId)
                                          && ua.ProjectName.Equals(userAssignmentTableEntity.ProjectName)
                                          && ua.ResourceFullName.Equals(userAssignmentTableEntity.ResourceFullName));
        }
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoTableEntitiesAreReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>(0);

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetProjectAsync(_projectId))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoProjectTableEntityReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetProjectAsync(_projectId))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoUserAssignmentTableEntityIsReturned_ReturnsProjectWithResourcesWhichHaveNoAssignment()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        var result = await _testee.GetProjectAsync(_projectId);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNull().And.HaveCount(1);
        result.Resources.Should().AllSatisfy(resource => resource.AssignedTo.Should().BeNullOrEmpty());
        result.Resources.Should().AllSatisfy(resource => resource.AssignedBy.Should().BeNullOrEmpty());
        result.Resources.Should().AllSatisfy(resource => resource.AssignedOnUtc.HasValue.Should().BeFalse());
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturned_ReturnsProjectWithResourcesWhichHaveAssignmentProperties()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        var result = await _testee.GetProjectAsync(_projectId);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNull().And.HaveCount(1);
        result.Resources.Single().AssignedTo.Should().Be(_userAssignment1.AssigneeUsername);
        result.Resources.Single().AssignedBy.Should().Be(_userAssignment1.AssignedByUsername);
        result.Resources.Single().AssignedOnUtc.Should().Be(_userAssignment1.AssignedOnUtc);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhook()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(Option<Project>.Some(_project));

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().NotBeNull();
        result!.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedUri()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(Option<Project>.Some(_project));

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedCustomProps()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(Option<Project>.Some(_project));

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.CustomProps.Should().NotBeNullOrEmpty();
        result!.CustomProps.Should().HaveCount(_webhookCustomProps.Count);
        result!.CustomProps!.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp1Name);
        //result!.CustomProps[_webhookCustomProp1Name].Should().Be(_webhookCustomProp1Value);
        result!.CustomProps!.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp2Name);
        //result!.CustomProps[_webhookCustomProp2Name].Should().Be(_webhookCustomProp2Value);
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        Option<Project> projectQueryResult = Option<Project>.None();
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenMatchingProjectFoundWithNoWebhookUri_ReturnsNull()
    {
        var project = new Project { Id = Guid.NewGuid() };

        Option<Project> projectQueryResult = Option<Project>.Some(project);
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetWebhookSpecificationAsync(project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsFalse()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(Option<Project>.Some(_project));

        var result = await _testee.IsNewProjectAsync(_project.Id);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsTrue()
    {
        Option<Project> projectQueryResult = Option<Project>.None();
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(projectQueryResult);

        var result = await _testee.IsNewProjectAsync(_project.Id);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenNoProjectWithCorrespondingIdIsFound_ThrowsArgumentException()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.None());

        await _testee.Invoking(t => t.RevokeUserAsync(_userAssignment1)).Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_PassesUserAssignmentWithProjectNameFromRetrievedProject()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _projectsTableService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectName.Equals(_project.Name))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectId()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectName()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_project.Name)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectEvent()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserRevoked)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    /*[TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectLevel()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Level.Equals(Levels.)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }*/

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectUsername()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_userAssignment1.AssignedByUsername)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithAssigneeUsernameInCustomProps()
    {
        const string propNameAssignedUsername = "revokedUsername";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameAssignedUsername)
                                                                && cws.CustomProps[propNameAssignedUsername].Equals(_assigneeUsername1))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithResourceFullNameInCustomProps()
    {
        const string propNameResourceFullName = "resourceFullName";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameResourceFullName)
                                                                && cws.CustomProps[propNameResourceFullName].Equals(_resource1FullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectToTableService()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _projectsTableService.StoreProjectAsync(_project)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenNameIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.Name = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.Name));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenNameIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.Name = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.Name)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenStateIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.State = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.State));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenStateIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.State = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.State)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenUsernameIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.Username = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.Username));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenUsernameIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.Username = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.Username)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_PassesProjectWithNewStateToTableService()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _projectsTableService.UpdateProjectStateAsync(_project.Id, _state2)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoMatchingProjectFoundById_DoesNotInvokeWebhook()
    {
        var invalidProjectId = Guid.NewGuid();

        await _testee.UpdateProjectStateAsync(invalidProjectId, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoMatchingProjectFoundById_DoesNotPassProjectToTableService()
    {
        var invalidProjectId = Guid.NewGuid();

        await _testee.UpdateProjectStateAsync(invalidProjectId, _state2);

        A.CallTo(() => _projectsTableService.UpdateProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoStateChange_DoesNotInvokeWebhook()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoStateChange_DoesNotPassProjectToTableService()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state1);

        A.CallTo(() => _projectsTableService.UpdateProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectStateTextInEventProperty()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)
                                                                                                          && cm.Event.Contains(Events.StateChange)
                                                                                                          && cm.Event.Contains(_state2)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
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
                Username = _username
            };
        }
    }
}
