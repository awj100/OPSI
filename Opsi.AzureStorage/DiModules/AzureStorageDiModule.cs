using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.AzureStorage.KeyPolicies;

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
            .AddTransient<IBlobService, BlobService>()
            .AddTransient<Common.ISettingsProvider, Common.SettingsProvider>()
            .AddTransient<IKeyPolicyFilterGeneration, KeyPolicyFilterGeneration>()
            .AddTransient<Func<string, IQueueService>>(provider => queueName =>
            {
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                return new QueueService(settingsProvider, queueName);
            })
            .AddTransient<Func<string, ITableService>>(provider => tableName =>
            {
                var keyPolicyFilterGeneration = provider.GetRequiredService<IKeyPolicyFilterGeneration>();
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                return new TableService(settingsProvider, tableName, keyPolicyFilterGeneration);
            })
            .AddTransient<IQueueServiceFactory, QueueServiceFactory>()
            .AddTransient<ITableServiceFactory, TableServiceFactory>();
    }
}
