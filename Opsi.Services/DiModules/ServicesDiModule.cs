using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Services.QueueHandlers.Dependencies;

namespace Opsi.Services.DiModules;

public static class ServicesDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        Configure(builder.Services);
    }

    public static void Configure(IServiceCollection services)
    {
        services
            .AddHttpClient()
            .AddLogging()
            .AddSingleton<ICallbackQueueService, CallbackQueueService>()
            .AddSingleton<IErrorQueueService, ErrorQueueService>()
            .AddSingleton<IManifestService, ManifestService>()
            .AddSingleton<IProjectsService, ProjectsService>()
            .AddSingleton<IProjectUploadService, ProjectUploadService>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<Func<Stream, IUnzipService>>(serviceProvider => stream => new UnzipService(stream))
            .AddSingleton<IUnzipServiceFactory, UnzipServiceFactory>()
            .AddTransient<IUserProvider, UserProvider>()
            .AddSingleton<QueueHandlers.IZippedQueueHandler, QueueHandlers.ZippedQueueHandler>();
    }
}
