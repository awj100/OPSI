using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IProjectsService
{
    Task<PageableResponse<Project>> GetProjectsAsync(string projectState, int pageSize, string? continuationToken = null);

    Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectStateAsync(Guid projectId, string newState);
}