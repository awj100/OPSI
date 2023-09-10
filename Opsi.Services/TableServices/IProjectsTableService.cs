using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services.TableServices;

public interface IProjectsTableService
{
    Task<Option<Project>> GetProjectByIdAsync(Guid projectId);

    Task<PageableResponse<OrderedProject>> GetProjectsByStateAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectAsync(Project project);

    /// <summary>
    /// Updates the project's <see cref="ProjectBase.State"/> and returns the updated <see cref="ProjectTableEntity"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="Option{ProjectTableEntity}"/> where <c>.IsSome</c> is <c>true</c> when a project has been updated.
    /// Otherwise <c>IsNone</c> is <c>true</c>, indicating that no project was updated.
    /// </returns>
    /// <exception cref="ArgumentException">No project with the specified <see cref="projectId"/> could be found.</exception>
    Task<Option<ProjectTableEntity>> UpdateProjectStateAsync(Guid projectId, string newState);
}
