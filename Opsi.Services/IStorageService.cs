using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

public interface IStorageService
{
    Task<Stream> RetrieveAsync(string name);

    Task StoreAsync(string fullName, Stream content);

    Task StoreVersionedFileAsync(ResourceStorageInfo resourceStorageInfo);
}