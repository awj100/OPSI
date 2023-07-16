using Opsi.Pocos;

namespace Opsi.Services;

public interface IProjectsService
{
    Task<IReadOnlyCollection<Project>> GetProjectsAsync(string projectState);

    Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task StoreProjectAsync(Project project);

    Task UpdateProjectStateAsync(Guid projectId, string newState);
}