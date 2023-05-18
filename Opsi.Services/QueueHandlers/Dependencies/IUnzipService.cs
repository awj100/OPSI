namespace Opsi.Services.QueueHandlers.Dependencies;

internal interface IUnzipService : IDisposable
{
    IReadOnlyCollection<string> GetFilePathsFromPackage();

    Task<Stream?> GetContentsAsync(string fullName);
}