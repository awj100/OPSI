using Azure.Storage.Blobs;
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

    public async Task<Stream> RetrieveAsync(string fullName)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task StoreAsync(string fullName, Stream content)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(fullName);

        await blobClient.UploadAsync(content, true);
    }

    public async Task<string> StoreVersionedFileAsync(ResourceStorageInfo resourceStorageInfo)
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

    private static async Task<string> StoreAsLatestAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var blobName = Path.Combine(resourceStorageInfo.ProjectId.ToString(), resourceStorageInfo.RestOfPath);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        return (await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true)).Value.VersionId;
    }

    private static async Task<string> StoreVersionAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var fileName = Path.GetFileName(resourceStorageInfo.RestOfPath);
        var versionedFileName = $"{resourceStorageInfo.VersionInfo.Index}.{fileName}";
        var pathWithoutFileName = resourceStorageInfo.RestOfPath.Substring(0, resourceStorageInfo.RestOfPath.Length - fileName.Length);
        var blobName = Path.Combine(resourceStorageInfo.ProjectId.ToString(), FolderNameVersions, pathWithoutFileName, versionedFileName);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        return (await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true)).Value.VersionId;
    }
}
