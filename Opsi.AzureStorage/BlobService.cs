using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;

namespace Opsi.AzureStorage;

internal class BlobService : StorageServiceBase, IBlobService
{
    private const string _containerName = "resources2";
    private readonly Lazy<BlobContainerClient> _containerClient;
    private readonly Lazy<BlobServiceClient> _serviceClient;

    public BlobService(ISettingsProvider settingsProvider) : base(settingsProvider)
    {
        _containerClient = new Lazy<BlobContainerClient>(GetContainerClient);
        _serviceClient = new Lazy<BlobServiceClient>(GetServiceClient);
    }

    public async Task DeleteAsync(string fullName)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task DeleteVersionAsync(string fullName, string versionId)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        var requestConditions = new BlobRequestConditions { IfMatch = new ETag(versionId) };

        await blobClient.DeleteAsync(DeleteSnapshotsOption.None, requestConditions);
    }

    public string GetBlobFullName(ResourceStorageInfo resourceStorageInfo)
    {
        return Path.Combine(resourceStorageInfo.ProjectId.ToString(), resourceStorageInfo.RestOfPath);
    }

    public async Task<IReadOnlyCollection<VersionInfo>> GetVersionInfos(string fullName)
    {
        var blobItems = new List<BlobItem>();

        Azure.AsyncPageable<BlobItem> blobs = _containerClient.Value.GetBlobsAsync(BlobTraits.Metadata, BlobStates.Version, prefix: fullName);
        IAsyncEnumerator<BlobItem> enumerator = blobs.GetAsyncEnumerator();
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                BlobItem blobItem = enumerator.Current;
                blobItems.Add(blobItem);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        // BlobItem.VersionId is a Timestamp - otder-by ascending puts the versions in chronological order.
        return blobItems.OrderBy(blobItem => blobItem.VersionId)
                        .Select((blobItem, idx) => new VersionInfo(blobItem.VersionId,
                                                                   idx,
                                                                   blobItem.Metadata.TryGetValue(Metadata.Assignee, out string? value)
                                                                       ? value
                                                                       : null))
                        .ToList();
    }

    public async Task<bool> RemovePropertiesAsync(string fullName, IEnumerable<string> propertyNames, bool shouldThrowIfNotExists = true)
    {
        var properties = await RetrieveBlobMetadataAsync(fullName, shouldThrowIfNotExists);

        var needsPersisted = true;
        foreach (var propertyName in propertyNames)
        {
            needsPersisted = needsPersisted && properties.Remove(propertyName);
        }

        if (needsPersisted)
        {
            var blobClient = _containerClient.Value.GetBlobClient(fullName);
            await blobClient.SetMetadataAsync(properties);
        }

        return needsPersisted;
    }

    public async Task<bool> RemovePropertyAsync(string fullName, string propertyName, bool shouldThrowIfNotExists = true)
    {
        return await RemovePropertiesAsync(fullName, [propertyName], shouldThrowIfNotExists);
    }

    public async Task<bool> RemoveTagAsync(string fullName, string tagName, bool shouldThrowIfNotExists = true)
    {
        var blobClient = _containerClient.Value.GetBlobClient(fullName);

        if (!await blobClient.ExistsAsync())
        {
            if (shouldThrowIfNotExists)
            {
                throw new ResourceNotFoundException(Guid.Empty, fullName);
            }

            return false;
        }

        var existingTags = (await blobClient.GetTagsAsync()).Value.Tags;
        if (!existingTags.Remove(tagName))
        {
            return false;
        }

        var response = await blobClient.SetTagsAsync(existingTags);

        return !response.IsError;
    }

    public BlobBaseClient RetrieveBlobClient(string fullName)
    {
        return _containerClient.Value.GetBlobClient(fullName);
    }

    public async Task<IReadOnlyCollection<BlobItem>> RetrieveBlobItemsInFolderAsync(string folderPath)
    {
        var blobItems = new List<BlobItem>();
        await foreach (var blobItem in _containerClient.Value.GetBlobsAsync(prefix: folderPath,
                                                                            states: BlobStates.Version,
                                                                            traits: BlobTraits.Metadata | BlobTraits.Tags))
        {
            blobItems.Add(blobItem);
        }

        return blobItems;
    }

    public async Task<IDictionary<string, string>> RetrieveBlobMetadataAsync(string fullName, bool shouldThrowIfNotExists = true)
    {
        var blobClient = RetrieveBlobClient(fullName);
        if (!await blobClient.ExistsAsync())
        {
            if (shouldThrowIfNotExists)
            {
                throw new ResourceNotFoundException(Guid.Empty, fullName);
            }

            return new Dictionary<string, string>(0);
        }

        var responseBlobProperties = await blobClient.GetPropertiesAsync();

        return responseBlobProperties.Value.Metadata;
    }

    public async Task<PageableResponse<BlobWithAttributes>> RetrieveByTagAsync(string tagName, string tagValue, int pageSize, string? continuationToken = null)
    {
        return await RetrieveByTagsAsync(new Dictionary<string, string> { { tagName, tagValue } }, pageSize, continuationToken);
    }

    public async Task<PageableResponse<BlobWithAttributes>> RetrieveByTagsAsync(IDictionary<string, string> tags, int pageSize, string? continuationToken = null)
    {
        var filterCondition = $"{String.Join(" AND ", tags.Select(tag => String.IsNullOrEmpty(tag.Value) ? $"{tag.Key}" : $"{tag.Key} = '{tag.Value}'"))}";

        var blobNames = new List<string>();
        string? thisPageContinuationToken = null;
        AsyncPageable<TaggedBlobItem> asyncPageable;
        try
        {
            asyncPageable = _containerClient.Value.FindBlobsByTagsAsync(filterCondition);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to retrieve by tag: {exception.Message}");
        }

        await foreach (Page<TaggedBlobItem> page in asyncPageable.AsPages(continuationToken, pageSize))
        {
            blobNames.AddRange(page.Values.Select(taggedBlobItem => taggedBlobItem.BlobName).ToList());
            thisPageContinuationToken = page.ContinuationToken;
        }

        var blobsWithAttributes = blobNames.Select(blobName => new BlobWithAttributes(blobName)).ToList();

        var tasksPopulateTags = blobsWithAttributes.Select(async blobWithAttributes =>
        {
            blobWithAttributes.Tags = await RetrieveTagsAsync(blobWithAttributes.Name);
        });

        await Task.WhenAll(tasksPopulateTags);

        var tasksPopulateMetadata = blobsWithAttributes.Select(async blobWithAttributes =>
        {
            blobWithAttributes.Metadata = await RetrieveBlobMetadataAsync(blobWithAttributes.Name);
        });

        await Task.WhenAll(tasksPopulateMetadata);

        return new PageableResponse<BlobWithAttributes>(blobsWithAttributes, thisPageContinuationToken);
    }

    public async Task<Stream?> RetrieveContentAsync(string fullName)
    {
        var blobClient = RetrieveBlobClient(fullName);
        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<IDictionary<string, string>> RetrieveTagsAsync(string fullName, bool shouldThrowIfNotExists = true)
    {
        var blobClient = RetrieveBlobClient(fullName);
        if (!await blobClient.ExistsAsync())
        {
            if (shouldThrowIfNotExists)
            {
                throw new Exception($"No blob could be found with the name \"{fullName}\".");
            }

            return new Dictionary<string, string>(0);
        }

        var responseBlobTags = await blobClient.GetTagsAsync();

        return responseBlobTags.Value.Tags;
    }

    public async Task SetMetadataAsync(string fullName, IDictionary<string, string> metadata)
    {
        if (!metadata.Any())
        {
            return;
        }

        var existingMetadata = await RetrieveBlobMetadataAsync(fullName, shouldThrowIfNotExists: true);

        foreach (var keyValuePair in metadata)
        {
            existingMetadata[keyValuePair.Key] = keyValuePair.Value;
        }

        var blobClient = _containerClient.Value.GetBlobClient(fullName);
        await blobClient.SetMetadataAsync(existingMetadata);
    }

    public async Task<bool> SetTagAsync(string fullName, string tagName, string? tagValue = null)
    {
        return await SetTagsAsync(fullName, new Dictionary<string, string> { { tagName, tagValue ?? String.Empty } });
    }

    public async Task<bool> SetTagsAsync(string fullName, IDictionary<string, string> tags)
    {
        var existingTags = await RetrieveTagsAsync(fullName);
        foreach (var tag in tags)
        {
            existingTags[tag.Key] = tag.Value;
        }

        var blobClient = _containerClient.Value.GetBlobClient(fullName);
        var response = await blobClient.SetTagsAsync(existingTags);

        return !response.IsError;
    }

    public async Task StoreResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await StoreResourceAsync(resourceStorageInfo.FullPath.Value, resourceStorageInfo.ContentStream);
    }

    public async Task StoreResourceAsync(string fullName, Stream content)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        try
        {
            await blobClient.UploadAsync(content, false);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to store the blob: {exception.Message}");
        }
    }

    public async Task<string> StoreVersionedResourceAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var containerClient = GetContainerClient();

        return await StoreAsLatestAsync(containerClient, resourceStorageInfo);
    }

    private BlobContainerClient GetContainerClient()
    {
        return _serviceClient.Value.GetBlobContainerClient(_containerName);
    }

    private BlobServiceClient GetServiceClient()
    {
        return new BlobServiceClient(StorageConnectionString.Value);
    }

    private async Task<string> StoreAsLatestAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var blobName = GetBlobFullName(resourceStorageInfo);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        Response<BlobContentInfo> response;
        try
        {
            response = await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to update the blob to a new version: {exception.Message}");
        }

        return response.Value.VersionId;
    }
}
