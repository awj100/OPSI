using System.Reflection;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services.TableServices;

internal class ProjectsTableService : IProjectsTableService
{
    private const string TableName = "resources";
    private readonly ITableService _projectsTableService;
    private readonly IProjectKeyPolicies _keyPolicies;
    private readonly IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;

    public ProjectsTableService(IProjectKeyPolicies keyPolicies,
                                ITableServiceFactory tableServiceFactory,
                                IKeyPolicyFilterGeneration keyPolicyFilterGeneration)
    {
        _projectsTableService = tableServiceFactory.Create(TableName);
        _keyPolicies = keyPolicies;
        _keyPolicyFilterGeneration = keyPolicyFilterGeneration;
    }

    public async Task<Option<Project>> GetProjectByIdAsync(Guid projectId)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (projectTableEntity.IsSome)
        {
            return Option<Project>.Some(projectTableEntity.Value.ToProject());
        }

        return Option<Project>.None();
    }

    public async Task<PageableResponse<Project>> GetProjectsByStateAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null)
    {
        var tableClient = _projectsTableService.TableClient.Value;
        var keyPolicies = _keyPolicies.GetKeyPoliciesByState(projectState);
        var propNamesToSelect = GetPropertyNames<ProjectTableEntity>();

        var keyPolicy = keyPolicies.SingleOrDefault(keyPolicy => keyPolicy.RowKey.Value.Contains(orderBy, StringComparison.OrdinalIgnoreCase));
        if (String.IsNullOrEmpty(keyPolicy.PartitionKey))
        {
            throw new Exception($"No key policy has been declared which matches the specified order-by (\"{orderBy}\").");
        }

        var pageResult = tableClient.QueryAsync<ProjectTableEntity>($"PartitionKey eq '{keyPolicy.PartitionKey}'",
                                                                    maxPerPage: pageSize,
                                                                    select: propNamesToSelect,
                                                                    cancellationToken: CancellationToken.None);

        if (pageResult == null)
        {
            return new PageableResponse<Project>(new List<Project>(0));
        }

        await foreach (var page in pageResult.AsPages(continuationToken))
        {
            var projects = page.Values.Select(projectTableEntity => projectTableEntity.ToProject()).ToList();
            return new PageableResponse<Project>(projects, page.ContinuationToken);
        }

        return new PageableResponse<Project>(new List<Project>(0));
    }

    public async Task StoreProjectAsync(Project project)
    {
        var projectTableEntities = ProjectTableEntity.FromProject(project, _keyPolicies.GetKeyPoliciesForStore);

        await _projectsTableService.StoreTableEntitiesAsync(projectTableEntities);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(project.Id);

        if (projectTableEntity.IsNone)
        {
            throw new InvalidOperationException($"Cannot update project with ID \"{project.Id}\" - no such project is stored");
        }

        foreach (var propInfo in typeof(ProjectBase).GetProperties(BindingFlags.Public| BindingFlags.Instance))
        {
            propInfo.SetValue(projectTableEntity.Value, propInfo.GetValue(project));
        }

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity.Value);
    }

    public async Task<Option<ProjectTableEntity>> UpdateProjectStateAsync(Guid projectId, string newState)
    {
        var optProjectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (optProjectTableEntity.IsNone)
        {
            throw new ArgumentException($"Cannot update project state: Project with ID \"{projectId}\" could not be found.", nameof(projectId));
        }

        var projectTableEntity = optProjectTableEntity.Value;
        var previousState = projectTableEntity.State;

        if (previousState.Equals(newState))
        {
            return Option<ProjectTableEntity>.None();
        }

        projectTableEntity.State = newState;

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity);

        var previousKeyPolicies = _keyPolicies.GetKeyPoliciesByState(previousState, projectId);
        await _projectsTableService.DeleteTableEntitiesAsync(previousKeyPolicies);

        var newKeyPolicies = _keyPolicies.GetKeyPoliciesByState(newState, projectId);
        foreach (var newKeyPolicy in newKeyPolicies)
        {
            projectTableEntity.PartitionKey = newKeyPolicy.PartitionKey;
            projectTableEntity.RowKey = newKeyPolicy.RowKey.Value;

            await _projectsTableService.StoreTableEntitiesAsync(projectTableEntity);
        }

        return Option<ProjectTableEntity>.Some(projectTableEntity);
    }

    private async Task<Option<ProjectTableEntity>> GetProjectTableEntityByIdAsync(Guid projectId)
    {
        const int maxResultsPerPage = 1;
        var propNamesToSelect = GetPropertyNames<ProjectTableEntity>();

        var keyPolicyForGet = _keyPolicies.GetKeyPolicyForGetById(projectId);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        var tableClient = _projectsTableService.TableClient.Value;

        var results = tableClient.QueryAsync<ProjectTableEntity>(keyPolicyFilter,
                                                                 maxPerPage: maxResultsPerPage,
                                                                 select: propNamesToSelect,
                                                                 cancellationToken: CancellationToken.None);

        await foreach (var result in results)
        {
            return Option<ProjectTableEntity>.Some(result);
        }

        return Option<ProjectTableEntity>.None();
    }

    private static IReadOnlyCollection<string> GetPropertyNames<TType>()
    {
        return typeof(TType).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(propInfo => propInfo.Name)
                            .ToList();
    }
}
