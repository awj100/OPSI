using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IResourceService
{
    Task<Option<ResourceContent>> GetResourceContentAsync(Guid projectId, string fullName);

    Task<bool> HasUserAccessAsync(Guid projectId, string fullName);

    Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);
}
