namespace Opsi.AzureStorage;

internal class QueueServiceFactory : IQueueServiceFactory
{
    private readonly Func<string, IQueueService> _queueServiceFactory;

    public QueueServiceFactory(Func<string, IQueueService> queueServiceFactory)
    {
        _queueServiceFactory = queueServiceFactory;
    }

    public IQueueService Create(string queueName)
    {
        return _queueServiceFactory(queueName);
    }
}
