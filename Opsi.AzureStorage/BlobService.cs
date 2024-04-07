using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Constants;

namespace Opsi.AzureStorage;

internal class BlobService : StorageServiceBase, IBlobService
{
    private const string ContainerName = "resources2";
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

        await blobClient.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
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

    public async Task<Stream> RetrieveContentAsync(string fullName)
    {
        var blobClient = RetrieveBlobClient(fullName);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public BlobBaseClient RetrieveBlobClient(string fullName)
    {
        return _containerClient.Value.GetBlobClient(fullName);
    }

    public async Task<IDictionary<string, string>> RetrieveBlobMetadataAsync(string fullName, bool throwIfNotExists = true)
    {
        var blobClient = RetrieveBlobClient(fullName);
        if (!await blobClient.ExistsAsync())
        {
            if (throwIfNotExists)
            {
                throw new Exception($"No blob could be found with the name \"{fullName}\".");
            }

            return new Dictionary<string, string>(0);
        }

        var responseBlobProperties = await blobClient.GetPropertiesAsync();

        return responseBlobProperties.Value.Metadata;
    }

    public async Task<IDictionary<string, string>> RetrieveTagsAsync(string fullName, bool throwIfNotExists = true)
    {
        var blobClient = RetrieveBlobClient(fullName);
        if (!await blobClient.ExistsAsync())
        {
            if (throwIfNotExists)
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

        var blobClient = _containerClient.Value.GetBlobClient(fullName);

        await blobClient.SetMetadataAsync(metadata);
    }

    public async Task<bool> SetTagAsync(string fullName, string tagName, string? tagValue = null)
    {
        return await SetTagsAsync(fullName, new Dictionary<string, string> { { tagName, tagValue ?? String.Empty } });
    }

    public async Task<bool> SetTagsAsync(string fullName, IDictionary<string, string> tags)
    {
        var blobClient = _containerClient.Value.GetBlobClient(fullName);

        var existingTags = (await blobClient.GetTagsAsync()).Value.Tags;
        foreach (var tag in tags)
        {
            if (!existingTags.ContainsKey(tag.Key))
            {
                existingTags.Add(tag);
            }
            else
            {
                existingTags[tag.Key] = tag.Value;
            }
        }

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
        return _serviceClient.Value.GetBlobContainerClient(ContainerName);
    }

    private BlobServiceClient GetServiceClient()
    {
        return new BlobServiceClient(StorageConnectionString.Value);
    }

    private static async Task<string> StoreAsLatestAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var blobName = Path.Combine(resourceStorageInfo.ProjectId.ToString(), resourceStorageInfo.RestOfPath);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        try
        {
            return (await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true)).Value.VersionId;
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to update the blob to a new version: {exception.Message}");
        }
    }
}
