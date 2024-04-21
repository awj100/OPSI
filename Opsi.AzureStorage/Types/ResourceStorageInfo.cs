namespace Opsi.AzureStorage.Types;

public record ResourceStorageInfo
{
    private readonly string _restOfPath;

    public ResourceStorageInfo(Guid projectId,
                               string restOfPath,
                               Stream contentStream,
                               string username)
    {
        _restOfPath = restOfPath;
        ContentStream = contentStream;
        ProjectId = projectId;
        RestOfPath = restOfPath;
        Username = username;

        BlobName = new Lazy<string>(() =>
        {
            var blobName = Path.Combine(projectId.ToString(), restOfPath);

            if (String.IsNullOrWhiteSpace(blobName))
            {
                throw new Exception($"Unable to build full blob name using {nameof(projectId)} = \"{projectId}\" and {nameof(restOfPath)} = \"{restOfPath}\".");
            }

            return blobName;
        });

        FileName = new Lazy<string>(() =>
        {
            var fileName = Path.GetFileName(restOfPath);

            if (String.IsNullOrWhiteSpace(fileName))
            {
                throw new Exception($"Unable to determine file name from path using {nameof(RestOfPath)} = \"{restOfPath}\".");
            }

            return fileName;
        });

        FullPath = new Lazy<string>(() =>
        {
            var fullPath = Path.Combine(projectId.ToString(), restOfPath.Substring(0, restOfPath.Length - FileName.Value.Length));

            if (String.IsNullOrWhiteSpace(fullPath))
            {
                throw new Exception($"Unable to build full storage path using {nameof(projectId)} = \"{projectId}\", {nameof(restOfPath)} = \"{restOfPath}\", {nameof(FileName)} = \"{FileName}\".");
            }

            return fullPath;
        });
    }

    /// <summary>
    /// Gets the full name of the resource blob - <em>i.e.</em>, 'directory' and resource name.
    /// </summary>
    /// <seealso cref="FileName"/>
    /// <seealso cref="FilePath"/>
    public Lazy<string> BlobName { get; }

    public Stream ContentStream { get; }

    /// <summary>
    /// Gets the name of the blob resource without the 'directory'.
    /// </summary>
    /// <seealso cref="BlobName"/>
    /// <seealso cref="FullPath"/>
    public Lazy<string> FileName { get; }

    /// <summary>
    /// Gets the path to the resource blob's containing 'directory'.
    /// </summary>
    /// <seealso cref="BlobName"/>
    /// <seealso cref="FileName"/>
    public Lazy<string> FullPath { get; }

    public Guid ProjectId { get; }

    public string RestOfPath { get; }

    public string Username { get; }

    public void ResetContentStream()
    {
        ContentStream.Position = 0;
    }

    public VersionedResourceStorageInfo ToVersionedResourceStorageInfo(VersionInfo versionInfo)
    {
        return new VersionedResourceStorageInfo(ProjectId,
                                                _restOfPath,
                                                ContentStream,
                                                Username,
                                                versionInfo);
    }
}
