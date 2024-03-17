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

        Task<IReadOnlyCollection<IGrouping<string, VersionedResourceStorageInfo>>> GetHistoryAsync(Guid projectId);

        Task<IReadOnlyCollection<VersionedResourceStorageInfo>> GetHistoryAsync(Guid projectId, string fullName);

        Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername);

        Task StoreResourceAsync(VersionedResourceStorageInfo versionedResourceStorageInfo);
    }
}