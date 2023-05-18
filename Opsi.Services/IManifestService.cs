using Opsi.Abstractions;
using Opsi.Pocos;

namespace Opsi.Services;

public interface IManifestService
{
    Task<Manifest> GetManifestAsync(IFormFileCollection formFiles);
}