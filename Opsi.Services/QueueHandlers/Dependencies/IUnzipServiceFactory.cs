namespace Opsi.Services.QueueHandlers.Dependencies;

public interface IUnzipServiceFactory
{
    IUnzipService Create(Stream stream);
}
