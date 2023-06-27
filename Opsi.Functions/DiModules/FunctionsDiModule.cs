using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Functions.FormHelpers;

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
                .AddSingleton<IMultipartFormDataParser, MultipartFormDataParser>();
    }
}
