using Azure.Storage.Queues;
using Opsi.Pocos;
using System.Text.Json;

namespace Opsi.AzureStorage;

internal class QueueService : IQueueService
{
    private readonly string _queueName;
    private readonly string _storageConnectionString;

    public QueueService(string storageConnectionString, string queueName)
    {
        _queueName = queueName;
        _storageConnectionString = storageConnectionString;
    }

    public async Task AddMessageAsync(Object obj)
    {
        var objectAsJson = JsonSerializer.Serialize(obj);
        var queueClient = GetQueueClient(_queueName);
        var queueExists = await EnsureQueueAsync(queueClient);

        if (queueExists)
        {
            await queueClient.SendMessageAsync(objectAsJson);
        }
    }

    private static async Task<bool> EnsureQueueAsync(QueueClient queueClient)
    {
        try
        {
            await queueClient.CreateIfNotExistsAsync();

            if (await queueClient.ExistsAsync())
            {
                return true;
            }
            else
            {
                Console.WriteLine($"Ensure the Azurite storage emulator running and try again.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}\n\n");
            Console.WriteLine($"Esure the Azurite storage emulator running and try again.");
            return false;
        }
    }

    private QueueClient GetQueueClient(string queueName)
    {
        return new QueueClient(_storageConnectionString, queueName);
    }
}
