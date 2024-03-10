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
}
