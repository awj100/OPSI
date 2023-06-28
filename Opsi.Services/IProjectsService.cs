using Opsi.AzureStorage.TableEntities;

namespace Opsi.Services;

public interface IProjectsService
{
    Task<string?> GetWebhookUriAsync(Guid projectId);

    Task<bool> IsNewProjectAsync(Guid projectId);

    Task StoreProjectAsync(Project project);
}