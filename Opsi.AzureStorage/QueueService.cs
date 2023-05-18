using System.Text.Json;
using Azure.Storage.Queues;
using Opsi.Common;

namespace Opsi.AzureStorage;

internal class QueueService : StorageServiceBase, IQueueService
{
    private readonly string _queueName;

    public QueueService(ISettingsProvider settingsProvider, string queueName) : base(settingsProvider)
    {
        _queueName = queueName;
    }

    public async Task AddMessageAsync(Object obj)
    {
        var objectAsJson = JsonSerializer.Serialize(obj);
        var queueClient = GetQueueClient();
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

    private QueueClient GetQueueClient()
    {
        return new QueueClient(StorageConnectionString.Value, _queueName);
    }
}
