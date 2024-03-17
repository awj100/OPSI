using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IResourceService
{
    Task<IReadOnlyCollection<VersionedResourceStorageInfo>> GetResourceHistoryAsync(Guid projectId, string restOfPath);

    Task<IReadOnlyCollection<IGrouping<string, VersionedResourceStorageInfo>>> GetResourcesHistoryAsync(Guid projectId);

    Task<Option<ResourceContent>> GetResourceContentAsync(Guid projectId, string fullName);

    Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername);

    Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);
}
