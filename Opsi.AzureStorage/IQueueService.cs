namespace Opsi.AzureStorage;

public interface IQueueService
{
    Task AddMessageAsync(Object obj);
}