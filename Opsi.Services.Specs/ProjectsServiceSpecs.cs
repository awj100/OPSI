using FakeItEasy;
using FluentAssertions;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _webhookCustomProp1Name = nameof(_webhookCustomProp1Name);
    private const string _webhookCustomProp1Value = nameof(_webhookCustomProp1Value);
    private const string _webhookCustomProp2Name = nameof(_webhookCustomProp2Name);
    private const int _webhookCustomProp2Value = 2;
    private const string _webhookUri = "https://a.test.url";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Project _project;
    private IProjectsTableService _projectsTableService;
    private IWebhookQueueService _webhookQueueService;
    private Dictionary<string, object> _webhookCustomProps;
    private ConsumerWebhookSpecification _webhookSpecs;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
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

        _project = new Project
        {
            Id = Guid.NewGuid(),
            WebhookSpecification = _webhookSpecs
        };

        _projectsTableService = A.Fake<IProjectsTableService>();
        _webhookQueueService = A.Fake<IWebhookQueueService>();

        _testee = new ProjectsService(_projectsTableService, _webhookQueueService);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhook()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().NotBeNull();
        result!.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedUri()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedCustomProps()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.CustomProps.Should().NotBeNullOrEmpty();
        result!.CustomProps.Should().HaveCount(_webhookCustomProps.Count);
        result!.CustomProps.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp1Name);
        //result!.CustomProps[_webhookCustomProp1Name].Should().Be(_webhookCustomProp1Value);
        result!.CustomProps.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp2Name);
        //result!.CustomProps[_webhookCustomProp2Name].Should().Be(_webhookCustomProp2Value);
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        Project? projectQueryResult = null;
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenMatchingProjectFoundWithNoWebhookUri_ReturnsNull()
    {
        var project = new Project { Id = Guid.NewGuid() };

        Project? projectQueryResult = project;
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetWebhookSpecificationAsync(project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsFalse()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.IsNewProjectAsync(_project.Id);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsTrue()
    {
        Project? projectQueryResult = null;
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
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(
            A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                && cws.CustomProps != null
                                                                && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(
            A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
    }
}
