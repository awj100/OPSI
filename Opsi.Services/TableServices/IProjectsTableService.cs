using Opsi.AzureStorage.TableEntities;

namespace Opsi.Services.TableServices;

public interface IProjectsTableService
{
    Task<Project?> GetProjectByIdAsync(Guid projectId);

    Task StoreProjectAsync(Project project);
}
