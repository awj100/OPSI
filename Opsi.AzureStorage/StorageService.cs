using Azure.Storage.Blobs;
using Opsi.AzureStorage.Types;

namespace Opsi.AzureStorage;

internal class StorageService : IStorageService
{
    private const string ContainerName = "resources";
    private const string FolderNameVersions = "versions";
    private readonly string _storageConnectionString;

    public StorageService(string storageConnectionString)
    {
        _storageConnectionString = storageConnectionString;
    }

    public async Task<Stream> RetrieveAsync(string name)
    {
        var containerClient = GetContainerClient();

        var blobClient = containerClient.GetBlobClient(name);

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

    public async Task StoreVersionedFileAsync(ResourceStorageInfo resourceStorageInfo)
    {
        var containerClient = GetContainerClient();

        await StoreAsLatestAsync(containerClient, resourceStorageInfo);

        await StoreVersionAsync(containerClient, resourceStorageInfo);
    }

    private BlobContainerClient GetContainerClient()
    {
        var blobServiceClient = new BlobServiceClient(_storageConnectionString);

        return blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    private static async Task StoreAsLatestAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var blobName = Path.Combine(resourceStorageInfo.ProjectId.ToString(), resourceStorageInfo.RestOfPath);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true);
    }

    private static async Task StoreVersionAsync(BlobContainerClient containerClient, ResourceStorageInfo resourceStorageInfo)
    {
        var fileName = Path.GetFileName(resourceStorageInfo.RestOfPath);
        var versionedFileName = $"{resourceStorageInfo.VersionInfo.Version}.{fileName}";
        var pathWithoutFileName = resourceStorageInfo.RestOfPath.Substring(0, resourceStorageInfo.RestOfPath.Length - fileName.Length);
        var blobName = Path.Combine(resourceStorageInfo.ProjectId.ToString(), FolderNameVersions, pathWithoutFileName, versionedFileName);

        var blobClient = containerClient.GetBlobClient(blobName);

        resourceStorageInfo.ResetContentStream();

        await blobClient.UploadAsync(resourceStorageInfo.ContentStream, true);
    }
}
