using System.Reflection;
using Opsi.AzureStorage;
using Opsi.AzureStorage.RowKeys;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services.TableServices;

internal class ProjectsTableService : IProjectsTableService
{
    private const string TableName = "resources";
    private readonly ITableService _projectsTableService;
    private readonly IProjectRowKeyPolicies _rowKeyPolicies;

    public ProjectsTableService(IProjectRowKeyPolicies rowKeyPolicies, ITableServiceFactory tableServiceFactory)
    {
        _projectsTableService = tableServiceFactory.Create(TableName);
        _rowKeyPolicies = rowKeyPolicies;
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

        var pageResult = tableClient.QueryAsync<ProjectTableEntity>($"{nameof(Project.State)} eq '{projectState}'",
                                                                    maxPerPage: pageSize,
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
        var projectTableEntities = ProjectTableEntity.FromProject(project, _rowKeyPolicies.GetRowKeysForCreate);

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
        IEnumerable<string>? selectProps = null;

        var tableClient = _projectsTableService.GetTableClient();

        var results = tableClient.QueryAsync<ProjectTableEntity>($"{nameof(Project.Id)} eq guid'{projectId}'",
                                                                 maxResultsPerPage,
                                                                 selectProps,
                                                                 CancellationToken.None);

        await foreach (var result in results)
        {
            return result;
        }

        return null;
    }
}
