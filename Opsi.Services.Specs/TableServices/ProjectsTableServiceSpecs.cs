using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class ProjectsTableServiceSpecs
{
    private const string Name = "TEST NAME";
    private const string PartitionKey = "PARTITION KEY";
    private const string RowKey1Filter = "ROW KEY 1 FILTER";
    private const string RowKey1Value = "ROW KEY 1";
    private const string RowKey2Value = "ROW KEY 2";
    private const string Username = "TEST USERNAME";

    private readonly string _nonReturnableState = ProjectStates.Deleted;
    private readonly string _returnableState = ProjectStates.InProgress;
    private readonly RowKey _rowKey1 = new(RowKey1Value, KeyPolicyQueryOperators.Equal);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;
    private Func<string, IReadOnlyCollection<KeyPolicy>> _getKeyPoliciesByState;
    private Func<string, Guid?, IReadOnlyCollection<KeyPolicy>> _getKeyPoliciesByStateAndId;
    private IReadOnlyCollection<KeyPolicy> _keyPoliciesForCreate;
    private KeyPolicy _keyPolicyForGet;
    private Project _project;
    private Guid _project1Id;
    private ProjectTableEntity _projectTableEntity1;
    private IProjectKeyPolicies _projectKeyPolicies;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ProjectsTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _keyPolicyFilterGeneration = A.Fake<IKeyPolicyFilterGeneration>();
        _getKeyPoliciesByState = state => new List<KeyPolicy> {
            new KeyPolicy($"{PartitionKey} {state}", _rowKey1),
            new KeyPolicy($"{PartitionKey} {state}", _rowKey1)
        };
        _getKeyPoliciesByStateAndId = (state, projectId) => new List<KeyPolicy> {
            new KeyPolicy($"{PartitionKey} {state}", new RowKey($"{_rowKey1.Value} {projectId}", KeyPolicyQueryOperators.Equal)),
            new KeyPolicy($"{PartitionKey} {state}", new RowKey($"{_rowKey1.Value} {projectId}", KeyPolicyQueryOperators.Equal))
        };
        _keyPoliciesForCreate = new List<KeyPolicy> {
            new KeyPolicy(PartitionKey, _rowKey1),
            new KeyPolicy(PartitionKey, _rowKey1)
        };
        _keyPolicyForGet = new KeyPolicy(PartitionKey, _rowKey1);
        _project1Id = Guid.NewGuid();
        _projectTableEntity1 = new ProjectTableEntity { Id = _project1Id, Name = Name, PartitionKey = PartitionKey, RowKey = Guid.NewGuid().ToString(), State = _returnableState, Username = Username };
        _project = _projectTableEntity1.ToProject();
        _projectKeyPolicies = A.Fake<IProjectKeyPolicies>();
        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _keyPolicyFilterGeneration.ToFilter(_keyPolicyForGet)).Returns(RowKey1Filter);
        A.CallTo(() => _projectKeyPolicies.GetKeyPoliciesByState(A<string>._, null)).ReturnsLazily((string state) => _getKeyPoliciesByState(state));
        A.CallTo(() => _projectKeyPolicies.GetKeyPoliciesByState(A<string>._, A<Guid?>._)).ReturnsLazily((string state, Guid? projectId) => _getKeyPoliciesByStateAndId(state, projectId));
        A.CallTo(() => _projectKeyPolicies.GetKeyPolicyForGetById(A<Guid>._)).Returns(_keyPolicyForGet);
        A.CallTo(() => _projectKeyPolicies.GetKeyPoliciesForStore(A<Project>._)).Returns(_keyPoliciesForCreate);
        A.CallTo(() => _tableService.TableClient).Returns(new Lazy<TableClient>(() => _tableClient));
        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new ProjectsTableService(_projectKeyPolicies, _tableServiceFactory, _keyPolicyFilterGeneration);
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenMatchingProjectFound_ReturnsProject()
    {
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_projectTableEntity1.Id);

        result.IsSome.Should().BeTrue();
        result.Value.Should().Match<Project>(m => m.Id.ToString().Equals(_projectTableEntity1.Id.ToString()));
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        var pages = GetQueryResponse();
        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>._,
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_projectTableEntity1.Id);

        result.IsNone.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetProjectsByStateAsync_WhenNoMatchingProjectsFound_ReturnsEmptyCollection()
    {
        var pages = GetQueryResponse();
        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>._,
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectsByStateAsync(_nonReturnableState, int.MaxValue);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectEntitiesWithSameIdToTableService()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IReadOnlyCollection<ProjectTableEntity>>.That.Matches(entities => entities.Select(entity => entity.Id)
                                                                                                                                         .Distinct()
                                                                                                                                         .Single()
                                                                                                                                         .Equals(_project.Id)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesRowKeyQuantityProjectsToTableService()
    {
        var expectedSavedEntityCount = _keyPoliciesForCreate.Count;

        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IReadOnlyCollection<ProjectTableEntity>>.That.Matches(entities => entities.Count == expectedSavedEntityCount))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrectPartitionKeyToTableService()
    {
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Contains(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntitiesAsync(A<ITableEntity>.That.Matches(tableEntity => tableEntity.PartitionKey.Equals(_projectTableEntity1.PartitionKey)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrectRowKeyToTableService()
    {
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntitiesAsync(A<ITableEntity>.That.Matches(tableEntity => tableEntity.RowKey.Equals(_projectTableEntity1.RowKey)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectWithCorrespondingProjectBaseProperties()
    {
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectAsync(_project);

        A.CallTo(() => _tableService.UpdateTableEntitiesAsync(A<ProjectTableEntity>.That.Matches(tableEntity => tableEntity.Id.Equals(_project.Id)
                                                                                                              && tableEntity.Name.Equals(_project.Name)
                                                                                                              && tableEntity.State.Equals(_project.State)
                                                                                                              && tableEntity.Username.Equals(_project.Username)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenProjectIdInvalid_ThrowsArgumentException()
    {
        var pages = GetQueryResponse();
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.Invoking(t => t.UpdateProjectStateAsync(_project1Id, "ANY STATE"))
                     .Should()
                     .ThrowAsync<ArgumentException>()
                     .WithParameterName("projectId");
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoChangeInState_ReturnsOptionIsNone()
    {
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, _projectTableEntity1.State);

        result.IsNone.Should().BeTrue();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_CallsTableServiceWithUpdatedProject()
    {
        var newState = _nonReturnableState;
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);

        A.CallTo(() => _tableService.UpdateTableEntitiesAsync(A<ProjectTableEntity>.That.Matches(pte => pte.Id.Equals(_projectTableEntity1.Id) && pte.State.Equals(newState)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_DeletesPreviousProjectsByStateKeys()
    {
        var newState = _nonReturnableState;
        var deleteKeyPolicies = _getKeyPoliciesByStateAndId(_projectTableEntity1.State, _projectTableEntity1.Id);
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);

        foreach (var deleteKeyPolicy in deleteKeyPolicies)
        {
            A.CallTo(() => _tableService.DeleteTableEntitiesAsync(A<IEnumerable<KeyPolicy>>.That.Matches(keyPolicies => keyPolicies.Any(keyPolicy => keyPolicy.PartitionKey.Equals(deleteKeyPolicy.PartitionKey)
                                                                                                                                                     && keyPolicy.RowKey.Equals(deleteKeyPolicy.RowKey)))))
             .MustHaveHappenedOnceExactly();
        }
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_AddsNewProjectsByStateKeys()
    {
        var newState = _nonReturnableState;
        var storeKeyPolicies = _getKeyPoliciesByStateAndId(newState, _projectTableEntity1.Id);
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);

        foreach (var storeKeyPolicy in storeKeyPolicies)
        {
            A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<ProjectTableEntity>.That.Matches(projectTableEntity => projectTableEntity.PartitionKey.Equals(storeKeyPolicy.PartitionKey))))
             .MustHaveHappened();

            A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<ProjectTableEntity>.That.Matches(projectTableEntity => projectTableEntity.RowKey.Equals(storeKeyPolicy.RowKey.Value))))
             .MustHaveHappened();
        }
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_ReturnsOptionIsSomeContainingProjectWithUpdatedState()
    {
        var newState = _nonReturnableState;
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);

        result.IsSome.Should().BeTrue();
    }

    private static AsyncPageable<ProjectTableEntity> GetQueryResponse(params ProjectTableEntity[] projectsResult)
    {
        var page = Page<ProjectTableEntity>.FromValues(projectsResult,
                                                       continuationToken: null,
                                                       response: A.Fake<Response>());
        return AsyncPageable<ProjectTableEntity>.FromPages(new[] { page });
    }
}
