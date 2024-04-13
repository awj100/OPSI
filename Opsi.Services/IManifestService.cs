using Opsi.Abstractions;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IManifestService
{
    Task<Manifest> ExtractManifestAsync(IFormFileCollection formFiles);

    string GetManifestFullName(Guid projectId);

    Task<InternalManifest?> RetrieveManifestAsync(Guid projectId);

    Task<string> StoreManifestAsync(InternalManifest internalManifest);
}