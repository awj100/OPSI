using Opsi.Common;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.AzureStorage
{
    public interface IProjectsService
    {
        Task<bool> IsNewProject(Guid projectId);

        Task StoreProjectAsync(Project project);
    }
}