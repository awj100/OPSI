using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.KeyPolicies;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

//[TestClass]
public class ProjectsTableServiceIntegrationSpecs
{
    private const string PartitionKey = "PARTITION KEY";
    private const int ProjectCount = 2002;
    private const string StorageConnectionStringName = "AzureWebJobsStorage";
    private const string StorageConnectionString = "UseDevelopmentStorage=true";
    private const string TableName = "projects";
    private const string Username = "user@test.com";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _defaultOrderBy = OrderBy.Desc;
    private IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;
    private List<Project> _projects;
    private string _projectState;
    private IProjectKeyPolicies _projectKeyPolicies;
    private List<ProjectTableEntity> _projectTableEntities;
    private IResourceKeyPolicies _resourceKeyPolicies;
    private ISettingsProvider _settingsProvider;
    private ITableEntityUtilities _tableEntityUtilities;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ProjectsTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestCleanup]
    public void TestCleanup()
    {
        _projectTableEntities.ForEach(async projectTableEntity => await _tableService.DeleteTableEntityAsync(projectTableEntity.PartitionKey, projectTableEntity.RowKey));
    }

    [TestInitialize]
    public async Task TestInit()
    {
        _keyPolicyFilterGeneration = new KeyPolicyFilterGeneration();
        _projectState = $"TEST-{Guid.NewGuid()}";
        _projectKeyPolicies = new ProjectKeyPolicies();
        _resourceKeyPolicies = A.Fake<IResourceKeyPolicies>();
        _settingsProvider = A.Fake<ISettingsProvider>();
        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s.Equals(StorageConnectionStringName)),
                                                  A<bool>._,
                                                  A<string>._)).Returns(StorageConnectionString);

        _tableEntityUtilities = new TableEntityUtilities();
        _tableService = new TableService(_settingsProvider, TableName, _keyPolicyFilterGeneration);
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new ProjectsTableService(_projectKeyPolicies,
                                           _resourceKeyPolicies,
                                           _tableServiceFactory,
                                           _keyPolicyFilterGeneration,
                                           _tableEntityUtilities);

        _projects = GenerateProjects().Take(ProjectCount).ToList();
        _projectTableEntities = _projects.Select(project => ProjectTableEntity.FromProject(project, PartitionKey, $"rowKey_{project.State}_{project.Id}")).ToList();

        _projectTableEntities.ForEach(async projectTableEntity => await _tableService.StoreTableEntitiesAsync(projectTableEntity));

        // Ensure all entities have been stored.
        await Task.Delay(10000);
    }

    [TestMethod]
    public async Task GetProjectsByStateAsync()
    {
        const int pageSize = 1000;

        var expectedFirstName1 = GenerateProjectName(2001);
        var expectedLastName1 = GenerateProjectName(1002);
        var expectedFirstName2 = GenerateProjectName(1001);
        var expectedLastName2 = GenerateProjectName(2);
        var expectedFirstName3 = GenerateProjectName(1);
        var expectedLastName3 = GenerateProjectName(0);

        var projectsByState = await _testee.GetProjectsByStateAsync(_projectState, _defaultOrderBy, pageSize, null);

        projectsByState.Items.Count.Should().Be(pageSize);
        projectsByState.ContinuationToken.Should().NotBeNullOrEmpty();
        projectsByState.Items[0].Name.Should().Be(expectedFirstName1);
        projectsByState.Items[projectsByState.Items.Count - 1].Name.Should().Be(expectedLastName1);

        projectsByState = await _testee.GetProjectsByStateAsync(_projectState, _defaultOrderBy, pageSize, projectsByState.ContinuationToken);

        projectsByState.Items.Count.Should().Be(pageSize);
        projectsByState.ContinuationToken.Should().NotBeNullOrEmpty();
        projectsByState.Items[0].Name.Should().Be(expectedFirstName2);
        projectsByState.Items[projectsByState.Items.Count - 1].Name.Should().Be(expectedLastName2);

        projectsByState = await _testee.GetProjectsByStateAsync(_projectState, _defaultOrderBy, pageSize, projectsByState.ContinuationToken);

        projectsByState.Items.Count.Should().Be(2);
        projectsByState.ContinuationToken.Should().BeNullOrEmpty();
        projectsByState.Items[0].Name.Should().Be(expectedFirstName3);
        projectsByState.Items[projectsByState.Items.Count - 1].Name.Should().Be(expectedLastName3);
    }

    private IEnumerable<Project> GenerateProjects()
    {
        var i = 0;

        while (true)
        {
            yield return new Project
            {
                Id = Guid.NewGuid(),
                Name = GenerateProjectName(i++),
                State = _projectState,
                Username = Username
            };
        }
    }

    private static string GenerateProjectName(int projectIndex)
    {
        return $"project_{projectIndex}";
    }
}
