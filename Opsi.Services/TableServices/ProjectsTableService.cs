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

    public async Task<Project?> GetProjectByIdAsync(Guid projectId)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (projectTableEntity != null)
        {
            return projectTableEntity.ToProject();
        }

        return null;
    }

    public async Task<PageableResponse<Project>> GetProjectsByStateAsync(string projectState, int pageSize, string? continuationToken = null)
    {
        var tableClient = _projectsTableService.GetTableClient();
        var keyPolicies = _keyPolicies.GetKeyPoliciesForGetByState(projectState);
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
        var projectTableEntity = await GetProjectTableEntityByIdAsync(project.Id) ?? throw new InvalidOperationException($"Cannot update project with ID \"{project.Id}\" - no such project is stored");

        foreach (var propInfo in typeof(ProjectBase).GetProperties(BindingFlags.Public| BindingFlags.Instance))
        {
            propInfo.SetValue(projectTableEntity, propInfo.GetValue(project));
        }

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity);
    }

    private async Task<ProjectTableEntity?> GetProjectTableEntityByIdAsync(Guid projectId)
    {
        const int maxResultsPerPage = 1;
        // TODO: Select only the properties on ProjectTableEntity.
        IEnumerable<string>? selectProps = null;

        var keyPolicyForGet = _keyPolicies.GetKeyPolicyForGet(projectId);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        var tableClient = _projectsTableService.GetTableClient();

        var results = tableClient.QueryAsync<ProjectTableEntity>(keyPolicyFilter,
                                                                 maxPerPage: maxResultsPerPage,
                                                                 select: selectProps,
                                                                 cancellationToken: CancellationToken.None);

        await foreach (var result in results)
        {
            return result;
        }

        return null;
    }
}
