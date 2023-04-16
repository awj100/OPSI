using System;
using Microsoft.Extensions.Logging;
using Opsi.Functions.Constants;
using Opsi.Functions.Dependencies;
using Opsi.Pocos;
using Opsi.Services;

namespace Opsi.Functions.BaseFunctions;

public abstract class FunctionWithStorageProvisions : FunctionWithConfiguration
{
    private readonly Lazy<Func<string, IQueueService>> ManifestQueueService;

    protected readonly Lazy<IProjectsService> ProjectsService;
    protected readonly Lazy<IQueueService> CallbackQueueService;
    protected readonly Lazy<IQueueService> ErrorQueueService;
    protected readonly Lazy<IResourcesService> ResourcesService;
    protected readonly Lazy<IStorageService> StorageService;

    protected FunctionWithStorageProvisions(IStorageFunctionDependencies storageFunctionDependencies)
    {
        CallbackQueueService = new Lazy<IQueueService>(() => ResolveQueueUsingConnectionString(storageFunctionDependencies.QueueServiceFactory, QueueNames.Callback));
        ErrorQueueService = new Lazy<IQueueService>(() => ResolveQueueUsingConnectionString(storageFunctionDependencies.QueueServiceFactory, QueueNames.Error));
        ManifestQueueService = new Lazy<Func<string, IQueueService>>(queueName => ResolveQueueUsingConnectionString(storageFunctionDependencies.QueueServiceFactory, queueName));
        ProjectsService = new Lazy<IProjectsService>(() => ResolveUsingConnectionString(storageFunctionDependencies.ProjectsServiceFactory));
        ResourcesService = new Lazy<IResourcesService>(() => ResolveUsingConnectionString(storageFunctionDependencies.ResourcesServiceFactory));
        StorageService = new Lazy<IStorageService>(() => ResolveUsingConnectionString(storageFunctionDependencies.StorageServiceFactory));
    }

    protected IQueueService GetManifestQueueService(Manifest manifest, ILogger log)
    {
        log.LogInformation($"{nameof(GetManifestQueueService)}: Resolving manifest queue for \"{manifest.HandlerQueue}\".");

        var manifestQueueName = GetManifestQueueName(manifest);

        try
        {
            return ManifestQueueService.Value(manifestQueueName);
        }
        catch (Exception ex)
        {
            var errorManifest = $"An error was encountered while resolving a queue service for \"{manifest.HandlerQueue}\".";
            log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
    }

    private T ResolveQueueUsingConnectionString<T>(Func<string, string, T> activatorFunc, string queueName)
    {
        if (!Initialised)
        {
            throw new Exception($"Cannot resolve {typeof(T).Name} before {typeof(FunctionWithStorageProvisions).Name}.{nameof(FunctionWithStorageProvisions.Init)} has been called.");
        }

        var connectionString = Configuration["AzureWebJobsStorage"];

        return activatorFunc(connectionString, queueName);
    }

    private T ResolveUsingConnectionString<T>(Func<string, T> activatorFunc)
    {
        if (!Initialised)
        {
            throw new Exception($"Cannot resolve {typeof(T).Name} before {typeof(FunctionWithStorageProvisions).Name}.{nameof(FunctionWithStorageProvisions.Init)} has been called.");
        }

        var connectionString = Configuration["AzureWebJobsStorage"];

        return activatorFunc(connectionString);
    }

    private static string GetManifestQueueName(Manifest manifest)
    {
        const string queuePrefix = "manifests";

        return $"{queuePrefix}-{manifest.HandlerQueue.ToLower()}";
    }
}
