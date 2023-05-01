using Azure.Data.Tables;
using Opsi.Common;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.AzureStorage;

public class ProjectsService : TableServiceBase, IProjectsService
{
    private const string TableName = "resources";

    public ProjectsService(string storageConnectionString) : base(storageConnectionString, TableName)
    {
    }

    public async Task<bool> IsNewProject(Guid projectId)
    {
        var key = projectId.ToString();

        var tableClient = GetTableClient();

        var results = tableClient.QueryAsync<Resource>(resource => resource.PartitionKey == key);

        await foreach (var result in results)
        {
            return false;
        }

        return true;
    }

    public async Task StoreProjectAsync(Project project)
    {
        await StoreTableEntityAsync(project);
    }
}
