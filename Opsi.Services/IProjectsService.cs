using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IProjectsService
{
    Task AssignUserAsync(UserAssignment userAssignment);

    Task<ProjectWithResources> GetAssignedProjectAsync(Guid projectId, string assigneeUsername);

    Task<IReadOnlyCollection<UserAssignment>> GetAssignedProjectsAsync(string assigneeUsername);

    Task<ProjectWithResources> GetProjectAsync(Guid projectId);

    Task<PageableResponse<OrderedProject>> GetProjectsAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null);

    Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId);

    Task InitProjectAsync(InternalManifest internalManifest);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task RevokeUserAsync(UserAssignment userAssignment);

    Task StoreProjectAsync(Project project);

    Task<bool> UpdateProjectStateAsync(Guid projectId, string newState);
}