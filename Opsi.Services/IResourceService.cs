using Opsi.AzureStorage.Types;

namespace Opsi.Services;

public interface IResourceService
{
    Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);
}
