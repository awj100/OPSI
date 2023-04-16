using Opsi.Services.Types;

namespace Opsi.Services;

public interface IStorageService
{
    Task<Stream> RetrieveAsync(string name);

    Task StoreAsync(string fullName, Stream content);

    Task StoreVersionedFileAsync(ResourceStorageInfo resourceStorageInfo);
}