using Opsi.Common;
using Opsi.Services.TableEntities;

namespace Opsi.Services
{
    public interface IProjectsService
    {
        Task<bool> IsNewProject(Guid projectId);

        Task StoreProjectAsync(Project project);
    }
}