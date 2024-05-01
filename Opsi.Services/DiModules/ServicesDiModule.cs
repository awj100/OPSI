using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Constants;
using Opsi.Services.Auth.OneTimeAuth;
using Opsi.Services.QueueHandlers.Dependencies;

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
            .AddLogging()
            .AddTransient<Func<Type, Auth.IAuthHandler?>>(serviceProvider => (Type type) => serviceProvider.GetRequiredService(type) as Auth.IAuthHandler)
            .AddTransient<Auth.IAuthHandlerProvider, Auth.AuthHandlerProvider>()
            .AddTransient<Auth.IAuthService, Auth.AuthService>()
            .AddTransient(typeof(Auth.OneTimeKeyAuthHandler))
            .AddTransient(typeof(Auth.ReferenceAuthHandler))
            .AddTransient<IManifestService, ManifestService>()  // singleton
            .AddTransient<IOneTimeAuthService, OneTimeAuthService>()
            .AddTransient<IOneTimeAuthKeyProvider, OneTimeAuthKeyProvider>()
            .AddTransient<IOneTimeAuthService, OneTimeAuthService>()
            .AddTransient<IProjectsService, ProjectsService>()
            .AddTransient<IProjectUploadService, ProjectUploadService>()
            .AddTransient<IResourceDispatcher, ResourceDispatcher>()
            .AddTransient<IResourceService, ResourceService>()
            .AddTransient<ITagUtilities, TagUtilities>()    // singleton
            .AddTransient<Func<Stream, IUnzipService>>(serviceProvider => stream => new UnzipService(stream))   // singleton
            .AddTransient<IUnzipServiceFactory, UnzipServiceFactory>()  // singleton
            .AddTransient<IUserInitialiser, UserProvider>()
            .AddTransient<IUserProvider, UserProvider>()
            .AddTransient<Func<FunctionContext, IUserProvider>>(_ => (FunctionContext functionContext) => new UserProvider(functionContext))
            .AddTransient<QueueHandlers.IZippedQueueHandler, QueueHandlers.ZippedQueueHandler>()
            .AddTransient<QueueServices.IWebhookQueueService, QueueServices.WebhookQueueService>()  // singleton
            .AddTransient<QueueServices.IErrorQueueService, QueueServices.ErrorQueueService>()  // singleton
            .AddTransient<TableServices.IOneTimeAuthKeysTableService, TableServices.OneTimeAuthKeysTableService>()
            .AddTransient<TableServices.ITableEntityUtilities, TableServices.TableEntityUtilities>()    // singleton
            .AddTransient<TableServices.IWebhookTableService, TableServices.WebhookTableService>()  // singleton
            .AddTransient<Webhooks.IWebhookDispatcher, Webhooks.WebhookDispatcher>()    // singleton
            .AddTransient<Webhooks.IWebhookService, Webhooks.WebhookService>(); // singleton

        services.AddHttpClient(HttpClientNames.OneTimeAuth, async (provider, httpClient) =>
        {
            try
            {
                var oneTimeAuthService = provider.GetRequiredService<IOneTimeAuthService>();
                var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
                var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);
                var userProvider = provider.GetRequiredService<IUserProvider>();

                httpClient.BaseAddress = new Uri(hostUrl);
                try
                {
                    httpClient.DefaultRequestHeaders.Authorization = await oneTimeAuthService.GetAuthenticationHeaderAsync(userProvider.Username, userProvider.IsAdministrator);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Unable to provide an HttpClient with one-time authentication: {exception.Message}");
                }
            }
            catch (Exception exception)
            {
                var m = exception.Message;
                throw;
            }
        });

        services.AddHttpClient(HttpClientNames.SelfWithContextAuth, (provider, httpClient) =>
        {
            var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
            var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);

            var userProvider = provider.GetRequiredService<IUserProvider>();
            var authHeader = userProvider.AuthHeader;

            httpClient.BaseAddress = new Uri(hostUrl);
            httpClient.DefaultRequestHeaders.Authorization = authHeader;
        });

        services.AddHttpClient(HttpClientNames.SelfWithoutAuth, (provider, httpClient) =>
        {
            var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
            var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);

            httpClient.BaseAddress = new Uri(hostUrl);
        });
    }
}
