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
            .AddHttpClient()
            .AddLogging()
            .AddSingleton<Func<Type, Auth.IAuthHandler?>>(serviceProvider => (Type type) => serviceProvider.GetRequiredService(type) as Auth.IAuthHandler)
            .AddSingleton<Auth.IAuthHandlerProvider, Auth.AuthHandlerProvider>()
            .AddSingleton<Auth.IAuthService, Auth.AuthService>()
            .AddSingleton(typeof(Auth.OneTimeKeyAuthHandler))
            .AddSingleton(typeof(Auth.ReferenceAuthHandler))
            .AddSingleton<AzureStorage.KeyPolicies.IProjectKeyPolicies, KeyPolicies.ProjectKeyPolicies>()
            .AddSingleton<AzureStorage.KeyPolicies.IResourceKeyPolicies, KeyPolicies.ResourceKeyPolicies>()
            .AddSingleton<IManifestService, ManifestService>()
            .AddSingleton<IOneTimeAuthService, OneTimeAuthService>()
            .AddSingleton<IOneTimeAuthKeyProvider, OneTimeAuthKeyProvider>()
            .AddSingleton<IOneTimeAuthService, OneTimeAuthService>()
            .AddSingleton<IProjectsService, ProjectsService>()
            .AddSingleton<IProjectUploadService, ProjectUploadService>()
            .AddSingleton<IResourceDispatcher, ResourceDispatcher>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<Func<Stream, IUnzipService>>(serviceProvider => stream => new UnzipService(stream))
            .AddSingleton<IUnzipServiceFactory, UnzipServiceFactory>()
            .AddTransient<IUserProvider, UserProvider>()
            .AddSingleton<Func<FunctionContext, IUserProvider>>(_ => (FunctionContext functionContext) => new UserProvider(functionContext))
            .AddSingleton<QueueHandlers.IZippedQueueHandler, QueueHandlers.ZippedQueueHandler>()
            .AddSingleton<QueueServices.IWebhookQueueService, QueueServices.WebhookQueueService>()
            .AddSingleton<QueueServices.IErrorQueueService, QueueServices.ErrorQueueService>()
            .AddSingleton<TableServices.IOneTimeAuthKeysTableService, TableServices.OneTimeAuthKeysTableService>()
            .AddSingleton<TableServices.IProjectsTableService, TableServices.ProjectsTableService>()
            .AddSingleton<TableServices.IWebhookTableService, TableServices.WebhookTableService>()
            .AddSingleton<Webhooks.IWebhookDispatcher, Webhooks.WebhookDispatcher>()
            .AddSingleton<Webhooks.IWebhookService, Webhooks.WebhookService>();

        services.AddHttpClient(HttpClientNames.OneTimeAuth, async (provider, httpClient) =>
        {
            var oneTimeAuthService = provider.GetRequiredService<IOneTimeAuthService>();
            var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
            var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);
            var userProvider = provider.GetRequiredService<IUserProvider>();

            httpClient.BaseAddress = new Uri(hostUrl);
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = await oneTimeAuthService.GetAuthenticationHeaderAsync(userProvider.Username.Value);
            }
            catch (Exception exception)
            {
                throw new Exception($"Unable to provide an HttpClient with one-time authentication: {exception.Message}");
            }
        });

        services.AddHttpClient(HttpClientNames.SelfWithContextAuth, (provider, httpClient) =>
        {
            var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
            var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);

            var userProvider = provider.GetRequiredService<IUserProvider>();
            var authHeader = userProvider.AuthHeader;

            httpClient.BaseAddress = new Uri(hostUrl);
            httpClient.DefaultRequestHeaders.Authorization = authHeader.Value;
        });

        services.AddHttpClient(HttpClientNames.SelfWithoutAuth, (provider, httpClient) =>
        {
            var settingsProvider = provider.GetRequiredService<Common.ISettingsProvider>();
            var hostUrl = settingsProvider.GetValue(ConfigKeys.HostUrl);

            httpClient.BaseAddress = new Uri(hostUrl);
        });
    }
}
