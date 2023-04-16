using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Opsi.TradosStudio.DiModules;

public static class TradosStudioDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<Func<Stream, IPackageService>>(serviceProvider => stream => 
        {
            return new PackageService(stream);
        });

        builder.Services.AddHttpClient();
    }
}
