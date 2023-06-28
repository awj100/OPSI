using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _username = "user@test.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Project _project;
    private IProjectsTableService _projectsTableService;
    private IUserProvider _userProvider;
    private IWebhookQueueService _webhookQueueService;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _project = new Project
        {
            Id = Guid.NewGuid(),
            WebhookUri = "https://test.com"
        };

        _projectsTableService = A.Fake<IProjectsTableService>();
        _userProvider = A.Fake<IUserProvider>();
        _webhookQueueService = A.Fake<IWebhookQueueService>();

        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _username));

        _testee = new ProjectsService(_projectsTableService, _userProvider, _webhookQueueService);
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenMatchingProjectFound_ReturnsProjectWebhook()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.GetWebhookUriAsync(_project.Id);

        result.Should().Be(_project.WebhookUri);
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        Project? projectQueryResult = null;
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetWebhookUriAsync(_project.Id);

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

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<InternalWebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<InternalWebhookMessage>.That.Matches(cm => cm.RemoteUri != null && cm.RemoteUri.Equals(_project.WebhookUri)))).MustHaveHappenedOnceExactly();
    }
}
