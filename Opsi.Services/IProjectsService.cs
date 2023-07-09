using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IProjectsService
{
    Task<ConsumerWebhookSpecification?> GetWebhookSpecificationAsync(Guid projectId);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task StoreProjectAsync(Project project);
}