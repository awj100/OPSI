using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    /// Removes properties (metadata) on a blob.
    /// </summary>
    /// <param name="fullName"The full path to the target blob.></param>
    /// <param name="propertyNames">A collection of property names to be removed.</param>
    /// <param name="shouldThrowIfNotExists">Indicates whether an exception should be thrown if the target blob doesn't exist.</param>
    /// <returns><c>true</c> indicates that the proeprties have been removed from the target blob; <c>false</c> otherwise (this case typically implies that the target blob doesn't exist or that one or more of the properties was not set on the blob).</returns>
    Task<bool> RemovePropertiesAsync(string fullName, IEnumerable<string> propertyNames, bool shouldThrowIfNotExists = true);

    /// <summary>
    /// Removes a property (metadata) on a blob.
    /// </summary>
    /// <param name="fullName"The full path to the target blob.></param>
    /// <param name="propertyName">The name of the property to be removed.</param>
    /// <param name="shouldThrowIfNotExists">Indicates whether an exception should be thrown if the target blob doesn't exist.</param>
    /// <returns><c>true</c> indicates that the proeprty has been removed from the target blob; <c>false</c> otherwise (this case typically implies that the target blob doesn't exist or that the property was not set on the blob).</returns>
    Task<bool> RemovePropertyAsync(string fullName, string propertyName, bool shouldThrowIfNotExists = true);

    /// <summary>
    /// Removes a tag on a blob.
    /// </summary>
    /// <param name="fullName"The full path to the target blob.></param>
    /// <param name="tagName">The name of the tag to be removed.</param>
    /// <param name="shouldThrowIfNotExists">Indicates whether an exception should be thrown if the target blob doesn't exist.</param>
    /// <returns><c>true</c> indicates that the tag has been removed from the target blob; <c>false</c> otherwise (this case typically implies that the target blob doesn't exist or that the tag was not set on the blob).</returns>
    Task<bool> RemoveTagAsync(string fullName, string tagName, bool shouldThrowIfNotExists = true);

    /// <summary>
    /// <para>Retrieves a collection of <see cref="BlobItem"/> objects within a folder, including all versions of versioned blobs.</para>
    /// <para>Each item will also specify its properties (metadata), versions, and tags.</para>
    /// <para>Versioned blobs will be returned in order from oldest to newest.</para>
    /// </summary>
    /// <param name="folderPath">The target folder path in which the blobs are stored.</param>
    Task<IReadOnlyCollection<BlobItem>> RetrieveBlobItemsInFolderAsync(string folderPath);

    /// <summary>
    /// Returns the <see cref="BlobClient"/> object representing the stored blob.
    /// </summary>
    /// <seealso cref="RetrieveContentAsync">This method returns only the blob's contents as a stream.</seealso>
    BlobBaseClient RetrieveBlobClient(string fullName);

    Task<IDictionary<string, string>> RetrieveBlobMetadataAsync(string fullName, bool shouldThrowIfNotExists = true);

    Task<PageableResponse<BlobWithAttributes>> RetrieveByTagAsync(string tagName, string tagValue, int pageSize, string? continuationToken = null);

    Task<PageableResponse<BlobWithAttributes>> RetrieveByTagsAsync(IDictionary<string, string> tags, int pageSize, string? continuationToken = null);

    /// <summary>
    /// Returns only the blob's contents as a stream.
    /// </summary>
    /// <seealso cref="RetrieveBlobClient">This method returns the <see cref="BlobClient"/>, which should be used if properties/metadata of the blob are also sought.</seealso>
    Task<Stream?> RetrieveContentAsync(string fullName);

    Task<IDictionary<string, string>> RetrieveTagsAsync(string fullName, bool shouldThrowIfNotExists = true);

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