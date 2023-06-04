using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.Services;

internal class ProjectsTableService : IProjectsTableService
{
    private const string TableName = "projects";
    private readonly ITableService _projectsTableService;

    public ProjectsTableService(ITableServiceFactory tableServiceFactory)
    {
        _projectsTableService = tableServiceFactory.Create(TableName);
    }

    public async Task<Project?> GetProjectByIdAsync(Guid projectId)
    {
        var tableClient = _projectsTableService.GetTableClient();

        var results = tableClient.QueryAsync<Project>(project => project.Id == projectId);

        await foreach (var result in results)
        {
            return result;
        }

        return null;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await _projectsTableService.StoreTableEntityAsync(project);
    }
}
