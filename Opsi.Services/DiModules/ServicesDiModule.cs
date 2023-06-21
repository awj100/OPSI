using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Services.Auth.OneTimeAuth;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.TableServices;

[assembly: InternalsVisibleTo("Opsi.Services.Specs")]

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
            .AddSingleton<Func<Type, Auth.IAuthHandler?>>(serviceProvider => (Type type) => serviceProvider.GetRequiredService(type) as Auth.IAuthHandler)
            .AddSingleton<Auth.IAuthHandlerProvider, Auth.AuthHandlerProvider>()
            .AddSingleton<Auth.IAuthService, Auth.AuthService>()
            .AddSingleton(typeof(Auth.OneTimeKeyAuthHandler))
            .AddSingleton(typeof(Auth.ReferenceAuthHandler))
            .AddSingleton<ICallbackQueueService, CallbackQueueService>()
            .AddSingleton<IErrorQueueService, ErrorQueueService>()
            .AddSingleton<IManifestService, ManifestService>()
            .AddSingleton<IOneTimeAuthService, OneTimeAuthService>()
            .AddSingleton<IOneTimeAuthKeyProvider, OneTimeAuthKeyProvider>()
            .AddSingleton<IOneTimeAuthKeysTableService, OneTimeAuthKeysTableService>()
            .AddSingleton<IOneTimeAuthService, OneTimeAuthService>()
            .AddSingleton<IProjectsService, ProjectsService>()
            .AddSingleton<IProjectsTableService, ProjectsTableService>()
            .AddSingleton<IProjectUploadService, ProjectUploadService>()
            .AddSingleton<IResourceDispatcher, ResourceDispatcher>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<Func<Stream, IUnzipService>>(serviceProvider => stream => new UnzipService(stream))
            .AddSingleton<IUnzipServiceFactory, UnzipServiceFactory>()
            .AddTransient<IUserProvider, UserProvider>()
            .AddSingleton<QueueHandlers.IZippedQueueHandler, QueueHandlers.ZippedQueueHandler>();
    }
}
