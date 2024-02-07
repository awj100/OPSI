using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage
{
    public interface IResourcesService
    {
        Task DeleteResourceAsync(ResourceStorageInfo resourceStorageInfo);

        Task DeleteResourceAsync(ResourceTableEntity resource);

        Task DeleteResourceAsync(Guid projectId, string fullName);

        /// <summary>
        /// The current version is the most recent version in this resource's history.
        /// </summary>
        Task<VersionInfo> GetCurrentVersionInfo(Guid projectId, string fullName);

        Task<IReadOnlyCollection<ResourceTableEntity>> GetResourcesAsync(Guid projectId);

        Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername);

        Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);
    }
}