namespace Opsi.Services.QueueHandlers.Dependencies;

internal interface IUnzipServiceFactory
{
    IUnzipService Create(Stream stream);
}
