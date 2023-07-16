using Opsi.Pocos;

namespace Opsi.Services.TableServices;

public interface IProjectsTableService
{
    Task<Project?> GetProjectByIdAsync(Guid projectId);

    Task<IReadOnlyCollection<Project>> GetProjectsByStateAsync(string projectState);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectAsync(Project project);
}
