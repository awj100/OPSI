using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IProjectsService
{
    Task AssignUserAsync(UserAssignment userAssignment);

    Task<ProjectWithResources?> GetProjectAsync(Guid projectId);

    Task<PageableResponse<OrderedProject>> GetProjectsAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null);

    Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task RevokeUserAsync(UserAssignment userAssignment);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectStateAsync(Guid projectId, string newState);
}