using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services.TableServices;

public interface IProjectsTableService
{
    Task<Project?> GetProjectByIdAsync(Guid projectId);

    Task<PageableResponse<Project>> GetProjectsByStateAsync(string projectState, int pageSize, string? continuationToken = null);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectAsync(Project project);
}
