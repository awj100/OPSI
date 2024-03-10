using Opsi.Abstractions;

namespace Opsi.Services;

public interface IProjectUploadService
{
    Task StoreInitialProjectUploadAsync(IFormFileCollection formCollection);
}