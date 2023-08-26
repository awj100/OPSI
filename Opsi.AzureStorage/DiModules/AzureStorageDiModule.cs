using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Opsi.Services.Specs")]
[assembly: InternalsVisibleTo("Opsi.AzureStorage.Specs")]

namespace Opsi.AzureStorage.DiModules;

public static class AzureStorageDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        Configure(builder.Services);
    }

    public static void Configure(IServiceCollection services)
    {
        services
            .AddSingleton<IBlobService, BlobService>()
            .AddSingleton<Common.ISettingsProvider, Common.SettingsProvider>()
            .AddSingleton<KeyPolicies.IKeyPolicyFilterGeneration, KeyPolicies.KeyPolicyFilterGeneration>()
            .AddSingleton<Func<string, IQueueService>>(provider => queueName =>
            {
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                return new QueueService(settingsProvider, queueName);
            })
            .AddSingleton<Func<string, ITableService>>(provider => tableName =>
            {
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                return new TableService(settingsProvider, tableName);
            })
            .AddSingleton<IQueueServiceFactory, QueueServiceFactory>()
            .AddSingleton<IResourcesService, ResourcesService>()
            .AddSingleton<ITableServiceFactory, TableServiceFactory>();
    }
}
