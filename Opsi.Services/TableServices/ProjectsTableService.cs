using System.Reflection;
using Azure.Data.Tables;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
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

    public async Task<PageableResponse<Project>> GetProjectsByStateAsync(string projectState, int pageSize, string? continuationToken = null)
    {
        var tableClient = _projectsTableService.TableClient.Value;
        var keyPolicies = _keyPolicies.GetKeyPoliciesByState(projectState);
        // TODO: Select only the properties on ProjectTableEntity.
        IEnumerable<string>? selectProps = null;

        var pageResult = tableClient.QueryAsync<ProjectTableEntity>($"{nameof(Project.State)} eq '{projectState}'",
                                                                    maxPerPage: pageSize,
                                                                    select: selectProps,
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
            propInfo.SetValue(projectTableEntity, propInfo.GetValue(project));
        }

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity.Value);
    }

    public async Task<Option<ProjectTableEntity>> UpdateProjectStateAsync(Guid projectId, string newState)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (projectTableEntity.IsNone)
        {
            throw new ArgumentException($"Cannot update project state: Project with ID \"{projectId}\" could not be found.", nameof(projectId));
        }

        var previousState = projectTableEntity.Value.State;

        if (previousState.Equals(newState))
        {
            return Option<ProjectTableEntity>.None();
        }

        projectTableEntity.Value.State = newState;

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity.Value);

        var previousKeyPolicies = _keyPolicies.GetKeyPoliciesByState(previousState, projectId);
        await _projectsTableService.DeleteTableEntitiesAsync(previousKeyPolicies);

        var newKeyPolicies = _keyPolicies.GetKeyPoliciesByState(newState, projectId);
        foreach (var newKeyPolicy in newKeyPolicies)
        {
            projectTableEntity.Value.PartitionKey = newKeyPolicy.PartitionKey;
            projectTableEntity.Value.RowKey = newKeyPolicy.RowKey.Value;

            await _projectsTableService.StoreTableEntitiesAsync(projectTableEntity.Value);
        }

        return Option<ProjectTableEntity>.Some(projectTableEntity.Value);
    }

    private async Task<Option<ProjectTableEntity>> GetProjectTableEntityByIdAsync(Guid projectId)
    {
        const int maxResultsPerPage = 1;
        // TODO: Select only the properties on ProjectTableEntity.
        IEnumerable<string>? selectProps = null;

        var keyPolicyForGet = _keyPolicies.GetKeyPolicyForGet(projectId);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        var tableClient = _projectsTableService.TableClient.Value;

        var results = tableClient.QueryAsync<ProjectTableEntity>(keyPolicyFilter,
                                                                 maxPerPage: maxResultsPerPage,
                                                                 select: selectProps,
                                                                 cancellationToken: CancellationToken.None);

        await foreach (var result in results)
        {
            return Option<ProjectTableEntity>.Some(result);
        }

        return Option<ProjectTableEntity>.None();
    }
}
