using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

public interface IBlobService
{
    Task DeleteAsync(string fullName);

    /// <summary>
    /// Returns only the blob's contents as a stream.
    /// </summary>
    /// <seealso cref="RetrieveBlob">This method returns the <see cref="BlobClient"/>, which should be used if properties/metadata of the blob are also sought.</seealso>
    Task<Stream> RetrieveContentAsync(string fullName);

    /// <summary>
    /// Returns the <see cref="BlobClient"/> object representing the stored blob.
    /// </summary>
    /// <seealso cref="RetrieveContentAsync">This method returns only the blob's contents as a stream.</seealso>
    BlobBaseClient RetrieveBlob(string fullName);

    Task StoreAsync(string fullName, Stream content);

    Task<string> StoreVersionedFileAsync(VersionedResourceStorageInfo resourceStorageInfo);
}