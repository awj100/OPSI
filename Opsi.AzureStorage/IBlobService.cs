using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

public interface IBlobService
{
    Task DeleteAsync(string fullName);

    Task<Stream> RetrieveAsync(string fullName);

    Task StoreAsync(string fullName, Stream content);

    Task<string> StoreVersionedFileAsync(ResourceStorageInfo resourceStorageInfo);
}