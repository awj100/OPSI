using System.Text.Json.Serialization;

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

    public void ResetContentStream()
    {
        ContentStream.Position = 0;
    }

    public override string ToString()
    {
        return $"{FullPath.Value}/{RestOfPath}{(String.IsNullOrEmpty(Username) ? String.Empty : $" {Username}")}";
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
