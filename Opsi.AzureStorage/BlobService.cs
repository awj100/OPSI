using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Opsi.AzureStorage.Types;
using Opsi.Common;

namespace Opsi.AzureStorage;

internal class BlobService : StorageServiceBase, IBlobService
{
    private const string ContainerName = "resources";
    private const string FolderNameVersions = "versions";

    public BlobService(ISettingsProvider settingsProvider) : base(settingsProvider)
    {
    }

    public async Task DeleteAsync(string fullName)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        await blobClient.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task<Stream> RetrieveContentAsync(string fullName)
    {
        var blobClient = RetrieveBlob(fullName);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public BlobBaseClient RetrieveBlob(string fullName)
    {
        var containerClient = GetContainerClient();

        return containerClient.GetBlobClient(fullName);
    }

    public async Task StoreAsync(string fullName, Stream content)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        await blobClient.UploadAsync(content, true);
    }

    public async Task<string> StoreVersionedFileAsync(VersionedResourceStorageInfo resourceStorageInfo)
    {
        var containerClient = GetContainerClient();

        return await StoreAsLatestAsync(containerClient, resourceStorageInfo);

        //await StoreVersionAsync(containerClient, resourceStorageInfo);
    }

    private BlobContainerClient GetContainerClient()
    {
        var blobServiceClient = new BlobServiceClient(StorageConnectionString.Value);

        return blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    private static async Task<string> StoreAsLatestAsync(BlobContainerClient containerClient, VersionedResourceStorageInfo versionedResourceStorageInfo)
    {
        var blobName = Path.Combine(versionedResourceStorageInfo.ProjectId.ToString(), versionedResourceStorageInfo.RestOfPath);

        var blobClient = containerClient.GetBlobClient(blobName);

        versionedResourceStorageInfo.ResetContentStream();

        return (await blobClient.UploadAsync(versionedResourceStorageInfo.ContentStream, true)).Value.VersionId;
    }

    private static async Task<string> StoreVersionAsync(BlobContainerClient containerClient, VersionedResourceStorageInfo versionedResourceStorageInfo)
    {
        var fileName = Path.GetFileName(versionedResourceStorageInfo.RestOfPath);
        var versionedFileName = $"{versionedResourceStorageInfo.VersionInfo.Index}.{fileName}";
        var pathWithoutFileName = versionedResourceStorageInfo.RestOfPath.Substring(0, versionedResourceStorageInfo.RestOfPath.Length - fileName.Length);
        var blobName = Path.Combine(versionedResourceStorageInfo.ProjectId.ToString(), FolderNameVersions, pathWithoutFileName, versionedFileName);

        var blobClient = containerClient.GetBlobClient(blobName);

        versionedResourceStorageInfo.ResetContentStream();

        return (await blobClient.UploadAsync(versionedResourceStorageInfo.ContentStream, true)).Value.VersionId;
    }
}
