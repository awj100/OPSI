using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Functions2.FormHelpers;

namespace Opsi.Functions2.DiModules;

public static class FunctionsDiModule
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
            .AddSingleton<Common.ISettingsProvider, Common.SettingsProvider>()
            .AddSingleton<IMultipartFormDataParser, MultipartFormDataParser>();
    }
}
