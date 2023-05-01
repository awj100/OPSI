using Microsoft.AspNetCore.Http;
using Opsi.Pocos;

namespace Opsi.AzureStorage;

public interface IManifestService
{
    Task<Manifest> GetManifestAsync(IFormFileCollection formFiles);
}