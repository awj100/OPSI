namespace Opsi.Services.QueueHandlers.Dependencies;

public interface IUnzipService : IDisposable
{
    IReadOnlyCollection<string> GetFilePathsFromPackage();

    Task<Stream?> GetContentsAsync(string fullName);
}