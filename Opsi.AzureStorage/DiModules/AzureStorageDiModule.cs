﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
            .AddSingleton<Common.ISettingsProvider, Common.SettingsProvider>()
            .AddSingleton<Func<string, IQueueService>>(provider => queueName =>
            {
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                return new QueueService(settingsProvider, queueName);
            })
            .AddSingleton<IQueueServiceFactory, QueueServiceFactory>()
            .AddSingleton<IResourcesService, ResourcesService>()
            .AddSingleton<IBlobService, BlobService>();
    }
}
