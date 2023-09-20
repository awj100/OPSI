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
    private const string RowKey1Filter = "ROW KEY 1 FILTER desc";
    private const string Username = "TEST USERNAME";

    private readonly string _defaultOrderBy = OrderBy.Asc;
    private readonly string _nonReturnableState = ProjectStates.Deleted;
    private readonly string _returnableState = ProjectStates.InProgress;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;
    private Func<string, IReadOnlyCollection<KeyPolicy>> _getKeyPoliciesByState;
    private IReadOnlyCollection<KeyPolicy> _keyPoliciesForCreate;
    private KeyPolicy _keyPolicyForGet;
    private Project _project;
    private Guid _project1Id;
    private IReadOnlyCollection<OrderedProjectTableEntity> _orderedProjectTableEntities;
    private ProjectTableEntity _projectTableEntity1;
    private IProjectKeyPolicies _projectKeyPolicies;
    private RowKey _rowKey1;
    private RowKey _rowKey2;
    private string _rowKey1Value;
    private string _rowKey2Value;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ProjectsTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _rowKey1Value = $"ROW KEY 1 {OrderBy.Asc}";
        _rowKey2Value = $"ROW KEY 2 {OrderBy.Desc}";
        _rowKey1 = new(_rowKey1Value, KeyPolicyQueryOperators.Equal);
        _rowKey2 = new(_rowKey2Value, KeyPolicyQueryOperators.Equal);

        _keyPolicyFilterGeneration = A.Fake<IKeyPolicyFilterGeneration>();
        _getKeyPoliciesByState = state => new List<KeyPolicy> {
            new KeyPolicy($"{PartitionKey} {state}", _rowKey1),
            new KeyPolicy($"{PartitionKey} {state}", _rowKey2)
        };
        _keyPoliciesForCreate = new List<KeyPolicy> {
            new KeyPolicy(PartitionKey, _rowKey1),
            new KeyPolicy(PartitionKey, _rowKey2)
        };
        _keyPolicyForGet = new KeyPolicy(PartitionKey, _rowKey1);
        _orderedProjectTableEntities = _getKeyPoliciesByState(_returnableState).Select(keyPolicy => new OrderedProjectTableEntity
        {
            Id = _project1Id,
            Name = Name,
            PartitionKey = keyPolicy.PartitionKey,
            RowKey = keyPolicy.RowKey.Value
        }).ToList();
        _project1Id = new Guid("cbd30af3-2ec9-4bc2-b719-296c149f66bb");
        _projectTableEntity1 = new ProjectTableEntity { Id = _project1Id, Name = Name, PartitionKey = PartitionKey, RowKey = Guid.NewGuid().ToString(), State = _returnableState, Username = Username };
        _project = _projectTableEntity1.ToProject();
        _projectKeyPolicies = A.Fake<IProjectKeyPolicies>();
        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _keyPolicyFilterGeneration.ToFilter(_keyPolicyForGet)).Returns(RowKey1Filter);
        A.CallTo(() => _projectKeyPolicies.GetKeyPoliciesByState(A<string>._)).ReturnsLazily((string state) => _getKeyPoliciesByState(state));
        A.CallTo(() => _projectKeyPolicies.GetKeyPolicyForGetById(A<Guid>._)).Returns(_keyPolicyForGet);
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
        var pages = GetQueryResponse<ProjectTableEntity>();
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
        var pages = GetQueryResponse<ProjectTableEntity>();
        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>._,
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectsByStateAsync(_nonReturnableState, _defaultOrderBy, int.MaxValue);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_QuantityOfTableEntitiesMatchesByIdKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        const int expectedProjectTableEntities = 1;

        tableEntities.Count(tableEntity => tableEntity is ProjectTableEntity)
                     .Should()
                     .Be(expectedProjectTableEntities);
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_PartitionKeyOfTableEntitiesMatchesByIdKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        tableEntities.Single(tableEntity => tableEntity is ProjectTableEntity).PartitionKey
                     .Should()
                     .Be(_keyPolicyForGet.PartitionKey);
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_RowKeyOfTableEntitiesMatchesByIdKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        tableEntities.Single(tableEntity => tableEntity is ProjectTableEntity).RowKey
                     .Should()
                     .Be(_keyPolicyForGet.RowKey.Value);
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_QuantityOfTableEntitiesMatchesByStateKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        var expectedOrderedProjectTableEntities = _getKeyPoliciesByState("random state").Count;

        tableEntities.Count(tableEntity => tableEntity is OrderedProjectTableEntity)
                     .Should()
                     .Be(expectedOrderedProjectTableEntities);
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_PartitionKeyOfTableEntitiesMatchesByStateKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        foreach (var keyPolicy in _getKeyPoliciesByState(_project.State))
        {
            tableEntities.Where(tableEntity => tableEntity is OrderedProjectTableEntity)
                         .FirstOrDefault(orderedProjectTableEntity => orderedProjectTableEntity.PartitionKey.Equals(keyPolicy.PartitionKey))
                         .Should()
                         .NotBeNull();
        }
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_RowKeyOfTableEntitiesMatchesByStateKeyPolicy()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        foreach (var keyPolicy in _getKeyPoliciesByState(_project.State))
        {
            tableEntities.Where(tableEntity => tableEntity is OrderedProjectTableEntity)
                         .SingleOrDefault(orderedProjectTableEntity => orderedProjectTableEntity.RowKey.Equals(keyPolicy.RowKey.Value))
                         .Should()
                         .NotBeNull();
        }
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenPassingTableEntitiesToTableService_PassesCorrectProjectId()
    {
        var tableEntities = new List<ITableEntity>();

#pragma warning disable CS8604 // Possible null reference argument.
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<IEnumerable<ITableEntity>>._)).Invokes(x =>
        {
            tableEntities.AddRange(x.GetArgument<IEnumerable<ITableEntity>>(0));
        });
#pragma warning restore CS8604 // Possible null reference argument.

        await _testee.StoreProjectAsync(_project);

        tableEntities.Where(tableEntity => tableEntity is ProjectTableEntity)
                     .Cast<ProjectTableEntity>()
                     .Should()
                     .AllSatisfy(projectTableEntity => projectTableEntity.Id.Equals(_project.Id));

        tableEntities.Where(tableEntity => tableEntity is OrderedProjectTableEntity)
                     .Cast<OrderedProjectTableEntity>()
                     .Should()
                     .AllSatisfy(projectTableEntity => projectTableEntity.Id.Equals(_project.Id));
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
        var pages = GetQueryResponse<ProjectTableEntity>();
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
    public async Task UpdateProjectStateAsync_DeletesPreviousEntitiesByStatePartitionKeys()
    {
        var previousState = _projectTableEntity1.State;
        var newState = _nonReturnableState;
        var KeyPoliciesByState = _getKeyPoliciesByState(_projectTableEntity1.State);

        // Fake the return value when getting the entity by ID.
        var pages1 = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGetById = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilterForGetById = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGetById);
        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilterForGetById)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages1);

        // Fake the return value when getting the entities by previous-state keys.
        var pages2 = GetQueryResponse(_orderedProjectTableEntities.ToArray());
        A.CallTo(() => _tableClient.QueryAsync<OrderedProjectTableEntity>(A<string>._,
                                                                          A<int?>._,
                                                                          A<IEnumerable<string>>._,
                                                                          A<CancellationToken>._)).Returns(pages2);

        await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);

        foreach (var keyPolicyByState in KeyPoliciesByState)
        {
            A.CallTo(() => _tableService.DeleteTableEntityAsync(A<string>.That.Matches(partitionKey => partitionKey.Equals(keyPolicyByState.PartitionKey)),
                                                                A<string>._))
             .MustHaveHappened();
        }
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_AddsNewProjectsByStateKeys()
    {
        var newState = _nonReturnableState;
        var storeKeyPolicies = _getKeyPoliciesByState(newState);
        var pages = GetQueryResponse(_projectTableEntity1);
        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(_projectTableEntity1.Id);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        A.CallTo(() => _tableClient.QueryAsync<ProjectTableEntity>(A<string>.That.Matches(filter => filter.Equals(keyPolicyFilter)),
                                                                   A<int?>._,
                                                                   A<IEnumerable<string>>._,
                                                                   A<CancellationToken>._)).Returns(pages);

        var usedKeys = new List<dynamic>();
        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<OrderedProjectTableEntity>._)).Invokes(x =>
        {
            var orderedProjectTableEntitiesArgs = x.GetArgument<ITableEntity[]>(0);
            if (orderedProjectTableEntitiesArgs == null)
            {
                return;
            }

            foreach (var orderedProjectTableEntityArg in orderedProjectTableEntitiesArgs)
            {
                usedKeys.Add(new
                {
                    orderedProjectTableEntityArg.PartitionKey,
                    orderedProjectTableEntityArg.RowKey
                });
            }
        });

        await _testee.UpdateProjectStateAsync(_projectTableEntity1.Id, newState);
        
        foreach (var storeKeyPolicy in storeKeyPolicies)
        {
            usedKeys.Should().Contain(new
            {
                storeKeyPolicy.PartitionKey,
                RowKey = storeKeyPolicy.RowKey.Value
            });
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

    private static AsyncPageable<TTableEntity> GetQueryResponse<TTableEntity>(params TTableEntity[] projectsResult) where TTableEntity : ITableEntity
    {
        var page = Page<TTableEntity>.FromValues(projectsResult,
                                                 continuationToken: null,
                                                 response: A.Fake<Response>());
        return AsyncPageable<TTableEntity>.FromPages(new[] { page });
    }
}
