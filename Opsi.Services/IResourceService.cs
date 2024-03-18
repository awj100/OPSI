using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Pocos;
using Opsi.Pocos.History;
using ResourceVersion = Opsi.Pocos.History.ResourceVersion;

namespace Opsi.Services;

public interface IResourceService
{
    Task<IReadOnlyCollection<ResourceVersion>> GetResourceHistoryAsync(Guid projectId, string restOfPath);

    Task<IReadOnlyCollection<GroupedResourceVersion>> GetResourcesHistoryAsync(Guid projectId);

    Task<Option<ResourceContent>> GetResourceContentAsync(Guid projectId, string fullName);

    Task<bool> HasUserAccessAsync(Guid projectId, string fullName, string requestingUsername);

    Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);
}
