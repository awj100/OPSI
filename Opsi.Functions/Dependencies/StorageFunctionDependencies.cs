using System;
using Opsi.AzureStorage;

namespace Opsi.Functions.Dependencies;

public class StorageFunctionDependencies : IStorageFunctionDependencies
{
    public StorageFunctionDependencies(
        Func<string, IProjectsService> projectsServiceFactory,
        Func<string, string, IQueueService> queueServiceFactory,
        Func<string, IResourcesService> resourcesServiceFactory,
        Func<string, IStorageService> storageServiceFactory)
    {
        ProjectsServiceFactory = projectsServiceFactory;
        QueueServiceFactory = queueServiceFactory;
        ResourcesServiceFactory = resourcesServiceFactory;
        StorageServiceFactory = storageServiceFactory;
    }

    public Func<string, IProjectsService> ProjectsServiceFactory { get; }

    public Func<string, string, IQueueService> QueueServiceFactory { get; }

    public Func<string, IResourcesService> ResourcesServiceFactory { get; }

    public Func<string, IStorageService> StorageServiceFactory { get; }
}
