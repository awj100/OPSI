using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Opsi.ErrorReporting.Services.Specs")]

namespace Opsi.ErrorReporting.Services.DiModules;

public static class ErrorServicesDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        Configure(builder.Services);
    }

    public static void Configure(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddSingleton<IErrorEmailService, ErrorEmailService>()
            .AddSingleton<IErrorService, ErrorService>()
            .AddSingleton<IErrorStorageService, ErrorStorageService>();
    }
}
