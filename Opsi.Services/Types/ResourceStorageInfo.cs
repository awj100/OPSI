using System;

namespace Opsi.AzureStorage.Types;

public readonly struct ResourceStorageInfo
{
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
    }

    public Stream ContentStream { get; }

    public Guid ProjectId { get; }

    public string RestOfPath { get; }

    public string Username { get; }

    public VersionInfo VersionInfo { get; }

    public void ResetContentStream()
    {
        ContentStream.Position = 0;
    }
}
