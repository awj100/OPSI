using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Constants;
using Opsi.Functions.FormHelpers;
using Opsi.Services;

[assembly: InternalsVisibleTo("Opsi.Services.Specs")]

namespace Opsi.Functions.DiModules;

public static class FunctionsDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        Configure(builder.Services);
    }

    public static void Configure(IServiceCollection services)
    {
        services.AddLogging()
                .AddSingleton<Common.ISettingsProvider, Common.SettingsProvider>()
                .AddSingleton<IMultipartFormDataParser, MultipartFormDataParser>()
                .AddHttpClient();

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
