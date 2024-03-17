namespace Opsi.AzureStorage.Types;

public record VersionedResourceStorageInfo : ResourceStorageInfo
{
    public VersionedResourceStorageInfo(Guid projectId,
                                        string restOfPath,
                                        Stream contentStream,
                                        string username,
                                        VersionInfo versionInfo) : base(projectId,
                                                                        restOfPath,
                                                                        contentStream,
                                                                        username)
    {
        VersionInfo = versionInfo;
    }

    public string? VersionId { get; set; }

    public VersionInfo VersionInfo { get; set; }

    public override string ToString()
    {
        return $"{FullPath.Value}/{RestOfPath} (v{VersionInfo.Index}){(String.IsNullOrEmpty(Username) ? String.Empty : $" {Username }")}";
    }
}
