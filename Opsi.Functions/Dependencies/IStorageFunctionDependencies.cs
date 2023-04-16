using System;
using Opsi.Services;

namespace Opsi.Functions.Dependencies;

public interface IStorageFunctionDependencies
{
    Func<string, IProjectsService> ProjectsServiceFactory { get; }
    Func<string, string, IQueueService> QueueServiceFactory { get; }
    Func<string, IResourcesService> ResourcesServiceFactory { get; }
    Func<string, IStorageService> StorageServiceFactory { get; }
}
