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
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _continuationToken = "TEST CONTINUATION TOKEN";
    private const string _name = "TEST NAME";
    private const int _pageSize = 10;
    private const string _state2 = "TEST STATE 2";
    private const string _username = "TEST USERNAME";
    private const string _webhookCustomProp1Name = nameof(_webhookCustomProp1Name);
    private const string _webhookCustomProp1Value = nameof(_webhookCustomProp1Value);
    private const string _webhookCustomProp2Name = nameof(_webhookCustomProp2Name);
    private const int _webhookCustomProp2Value = 2;
    private const string _webhookUri = "https://a.test.url";
    private readonly Guid _projectId = Guid.NewGuid();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _defaultOrderBy = OrderBy.Desc;
    private OrderedProject _orderedProject;
    private Project _project;
    private IProjectsTableService _projectsTableService;
    private readonly string _state1 = ProjectStates.InProgress;
    private ILoggerFactory _loggerFactory;
    private IResourcesService _resourcesService;
    private IUserProvider _userProvider;
    private IWebhookQueueService _webhookQueueService;
    private Dictionary<string, object> _webhookCustomProps;
    private ConsumerWebhookSpecification _webhookSpecs;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
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
            Name = _name
        };

        _project = new Project
        {
            Id = _projectId,
            Name = _name,
            State = _state1,
            Username = _username,
            WebhookSpecification = _webhookSpecs
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
    public async Task GetProjectAsync_WhenProjectIdNotRecognised_ReturnsNull()
    {
        var response = await _testee.GetProjectAsync(Guid.NewGuid());

        response.Should().BeNull();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoResourcesObtained_ReturnsNull()
    {
        A.CallTo(() => _resourcesService.GetResourcesAsync(Guid.NewGuid())).Returns(new List<ResourceTableEntity>(0));

        var response = await _testee.GetProjectAsync(_projectId);

        response.Should().BeNull();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenUserIsAdministrator_ReturnsProjectWithAllResources()
    {
        const bool isAdministrator = true;
        const string otherUsername = "otherUser@test.com";
        var resources = GenerateResources().Take(4).ToList();
        resources.ElementAt(1).Username = otherUsername;
        resources.ElementAt(3).Username = otherUsername;

        A.CallTo(() => _resourcesService.GetResourcesAsync(_projectId)).Returns(resources);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(new Lazy<bool>(() => isAdministrator));

        var response = await _testee.GetProjectAsync(_projectId);

        response.Should().NotBeNull();
        response!.Resources.Should().NotBeNullOrEmpty();
        response!.Resources.Should().HaveCount(resources.Count);
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenUserIsNotAdministrator_ReturnsProjectWithOnlyUserAssignedResources()
    {
        const bool isAdministrator = false;
        const string otherUsername = "otherUser@test.com";
        var resources = GenerateResources().Take(4).ToList();
        resources.ElementAt(1).Username = otherUsername;
        resources.ElementAt(3).Username = otherUsername;

        A.CallTo(() => _resourcesService.GetResourcesAsync(_projectId)).Returns(resources);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(new Lazy<bool>(() => isAdministrator));

        var response = await _testee.GetProjectAsync(_projectId);

        response.Should().NotBeNull();
        response!.Resources.Should().NotBeNullOrEmpty();
        response!.Resources.Should().HaveCount(resources.Count(resource => resource.Username != null && resource.Username.Equals(_username)));
        response!.Resources.Should().AllSatisfy(resource => resource.Username.Should().Be(_username));
    }

    [TestMethod]
    public async Task GetProjectsAsync_WhenProjectStateIsRecognised_ReturnsResultFromTableService()
    {
        var pageableResponse = new PageableResponse<OrderedProject>(new List<OrderedProject> { _orderedProject }, _continuationToken);

        A.CallTo(() => _projectsTableService.GetProjectsByStateAsync(_state1, _defaultOrderBy, _pageSize, A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetProjectsAsync(_state1, _defaultOrderBy, _pageSize);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(1)
              .And.Match(projects => projects.Single().Id.Equals(_project.Id));
    }

    [TestMethod]
    public async Task GetProjectsAsync_WhenProjectStateIsNoRecognised_ThrowsArgumentException()
    {
        await _testee.Invoking(t => t.GetProjectsAsync(Guid.NewGuid().ToString(), _defaultOrderBy, _pageSize, _continuationToken))
                     .Should()
                     .ThrowAsync<ArgumentException>()
                     .WithParameterName("projectState");
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
                Username = _username,
                VersionId = $"VERSION ID {i}",
                VersionIndex = i
            };
        }
    }
}
