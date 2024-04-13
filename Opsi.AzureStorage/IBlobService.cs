using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Opsi.AzureStorage.Types;
using Opsi.Common;

namespace Opsi.AzureStorage;

public interface IBlobService
{
    Task DeleteAsync(string fullName);

    /// <summary>
    /// Returns a collection of <see cref="VersionInfo"/> objects, ordered chronologically.
    /// </summary>
    /// <param name="fullName">The full path to the target blob.</param>
    Task<IReadOnlyCollection<VersionInfo>> GetVersionInfos(string fullName);

    /// <summary>
    /// Returns only the blob's contents as a stream.
    /// </summary>
    /// <seealso cref="RetrieveBlobClient">This method returns the <see cref="BlobClient"/>, which should be used if properties/metadata of the blob are also sought.</seealso>
    Task<Stream?> RetrieveContentAsync(string fullName);

    /// <summary>
    /// Returns the <see cref="BlobClient"/> object representing the stored blob.
    /// </summary>
    /// <seealso cref="RetrieveContentAsync">This method returns only the blob's contents as a stream.</seealso>
    BlobBaseClient RetrieveBlobClient(string fullName);

    Task<IDictionary<string, string>> RetrieveBlobMetadataAsync(string fullName, bool throwIfNotExists = true);

    Task<PageableResponse<BlobClient>> RetrieveByTagAsync(IDictionary<string, string> tags, int pageSize, string? continuationToken = null);

    Task<IDictionary<string, string>> RetrieveTagsAsync(string fullName, bool throwIfNotExists = true);

    Task SetMetadataAsync(string fullName, IDictionary<string, string> metadata);

    /// <summary>
    /// Sets a tag on a blob.
    /// </summary>
    /// <param name="fullName">The full path to the target blob.</param>
    /// <param name="tagName">The name of the tag to be set.</param>
    /// <param name="tagValue">(Optional) The value of the tag to be set.</param>
    /// <returns><c>true</c> if the tag was set on the target blob; <c>false</c> otherwise.</returns>
    Task<bool> SetTagAsync(string fullName, string tagName, string? tagValue = null);

    /// <summary>
    /// Sets tags on a blob.
    /// </summary>
    /// <param name="fullName">The full path to the target blob.</param>
    /// <param name="tags">A collection of tag names and corresponding values to be set.</param>
    /// <returns><c>true</c> if the tags were set on the target blob; <c>false</c> otherwise.</returns>
    Task<bool> SetTagsAsync(string fullName, IDictionary<string, string> tags);

    Task StoreResourceAsync(string fullName, Stream content);

    Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo);

    Task<string> StoreVersionedResourceAsync(ResourceStorageInfo resourceStorageInfo);
}