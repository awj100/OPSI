namespace Opsi.AzureStorage.Types;

public class ResourceStorageInfo
{
    public ResourceStorageInfo(Guid projectId,
                               string restOfPath,
                               Stream contentStream,
                               string username) : this(projectId,
                                                       restOfPath,
                                                       contentStream,
                                                       new VersionInfo(1),
                                                       username)
    {
    }

    public ResourceStorageInfo(Guid projectId,
                               string restOfPath,
                               Stream contentStream,
                               VersionInfo versionInfo,
                               string username)
    {
        ContentStream = contentStream;
        ProjectId = projectId;
        RestOfPath = restOfPath;
        Username = username;
        VersionInfo = versionInfo;

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

    public Stream ContentStream { get; }

    public Lazy<string> FileName { get; }

    public Lazy<string> FullPath { get; }

    public Guid ProjectId { get; }

    public string RestOfPath { get; }

    public string Username { get; }

    public string? VersionId { get; set; }

    public VersionInfo VersionInfo { get; set; }

    public void ResetContentStream()
    {
        ContentStream.Position = 0;
    }
}
