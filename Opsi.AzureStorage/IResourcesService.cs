using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage
{
    public interface IResourcesService
    {
        Task DeleteResourceAsync(ResourceStorageInfo resourceStorageInfo);

        Task DeleteResourceAsync(Resource resource);

        Task DeleteResourceAsync(Guid projectId, string fullName);

        /// <summary>
        /// The current version is the most recent version in this resource's history.
        /// </summary>
        Task<VersionInfo> GetCurrentVersionInfo(Guid projectId, string fullName);

        Task<IReadOnlyCollection<Resource>> GetResourcesAsync(Guid projectId);

        Task LockResourceToUser(Guid projectId, string fullName, string username);

        Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);

        Task UnlockResource(Guid projectId, string fullName);

        Task UnlockResourceFromUser(Guid projectId, string fullName, string username);
    }
}