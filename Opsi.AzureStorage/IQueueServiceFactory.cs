namespace Opsi.AzureStorage;

public interface IQueueServiceFactory
{
    IQueueService Create(string queueName);
}
