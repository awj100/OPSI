namespace Opsi.Services.QueueHandlers.Dependencies;

internal class UnzipServiceFactory : IUnzipServiceFactory
{
    private readonly Func<Stream, IUnzipService> _unzipServiceFactoryFunc;

    public UnzipServiceFactory(Func<Stream, IUnzipService> unzipServiceFactoryFunc)
    {
        _unzipServiceFactoryFunc = unzipServiceFactoryFunc;
    }

    public IUnzipService Create(Stream stream)
    {
        return _unzipServiceFactoryFunc(stream);
    }
}
