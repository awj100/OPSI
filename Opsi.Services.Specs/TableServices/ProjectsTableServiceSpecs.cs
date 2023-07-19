using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class ProjectsTableServiceSpecs
{
    private const string Name = "TEST NAME";
    private const string Username = "TEST USERNAME";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _nonReturnableState = ProjectStates.Deleted;
    private Project _project;
    private Guid _project1Id;
    private Guid _project2Id;
    private ProjectTableEntity _projectTableEntity1;
    private ProjectTableEntity _projectTableEntity2;
    private string _returnableState = ProjectStates.InProgress;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ProjectsTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _project1Id = Guid.NewGuid();
        _project2Id = Guid.NewGuid();
        _projectTableEntity1 = new ProjectTableEntity { Id = _project1Id, Name = Name, State = _returnableState, Username = Username };
        _projectTableEntity2 = new ProjectTableEntity { Id = _project2Id, Name = Name, State = _returnableState, Username = Username };
        _project = _projectTableEntity1.ToProject();
        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _tableService.GetTableClient()).Returns(_tableClient);
        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new ProjectsTableService(_tableServiceFactory);
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenMatchingProjectFound_ReturnsProject()
    {
        var projectsResult = new List<ProjectTableEntity> { _projectTableEntity1 };
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(_projectTableEntity1.Id.ToString())),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_projectTableEntity1.Id);

        result.Should()
              .NotBeNull()
              .And.Match<Project>(m => m.Id.ToString().Equals(_projectTableEntity1.Id.ToString()));
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        var newProject = new ProjectTableEntity { Id = Guid.NewGuid() };
        var projectsResult = new List<ProjectTableEntity> { newProject };
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(newProject.Id.ToString())),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_projectTableEntity1.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetProjectsByStateAsync_WhenNoMatchingProjectsFound_ReturnsEmptyCollection()
    {
        var projectsResult = new List<ProjectTableEntity>(0);
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.State)) && filter.Contains(_nonReturnableState)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectsByStateAsync(_nonReturnableState, int.MaxValue);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectToTableService()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _tableService.StoreTableEntityAsync(A<ProjectTableEntity>.That.Matches(pte => pte.Id.Equals(_project.Id)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrectPartitionKeyToTableService()
    {
        var projectsResult = new List<ProjectTableEntity> { _projectTableEntity1 };
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(_projectTableEntity1.Id.ToString())),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntityAsync(A<ITableEntity>.That.Matches(tableEntity => tableEntity.PartitionKey.Equals(_projectTableEntity1.PartitionKey)))).MustHaveHappenedOnceExactly();
        }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrectRowKeyToTableService()
    {
        var projectsResult = new List<ProjectTableEntity> { _projectTableEntity1 };
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(_projectTableEntity1.Id.ToString())),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntityAsync(A<ITableEntity>.That.Matches(tableEntity => tableEntity.RowKey.Equals(_projectTableEntity1.RowKey)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrespondingProjectBaseProperties()
    {
        var projectsResult = new List<ProjectTableEntity> { _projectTableEntity1 };
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        var pages = AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(_projectTableEntity1.Id.ToString())),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntityAsync(A<ProjectTableEntity>.That.Matches(tableEntity => tableEntity.Id.Equals(_project.Id)
                                                                                                              && tableEntity.Name.Equals(_project.Name)
                                                                                                              && tableEntity.State.Equals(_project.State)
                                                                                                              && tableEntity.Username.Equals(_project.Username)))).MustHaveHappenedOnceExactly();
    }
}
