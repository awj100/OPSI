using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ICallbackQueueService _callbackQueueService;
    private Project _project;
    private IProjectsTableService _projectsTableService;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _project = new Project
        {
            CallbackUri = "https://test.com",
            Id = Guid.NewGuid()
        };

        _callbackQueueService = A.Fake<ICallbackQueueService>();
        _projectsTableService = A.Fake<IProjectsTableService>();

        _testee = new ProjectsService(_projectsTableService, _callbackQueueService);
    }

    [TestMethod]
    public async Task GetCallbackUriAsync_WhenMatchingProjectFound_ReturnsProjectCallback()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(_project);

        var result = await _testee.GetCallbackUriAsync(_project.Id);

        result.Should().Be(_project.CallbackUri);
    }

    [TestMethod]
    public async Task GetCallbackUriAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        Project? projectQueryResult = null;
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_project.Id)))).Returns(projectQueryResult);

        var result = await _testee.GetCallbackUriAsync(_project.Id);

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
    public async Task StoreProjectAsync_InvokesCallback()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)))).MustHaveHappenedOnceExactly();
    }
}
