using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Opsi.Functions.DiModules;
using Opsi.Services.DiModules;
using Opsi.TradosStudio.DiModules;

[assembly: FunctionsStartup(typeof(Opsi.Functions.Startup))]

namespace Opsi.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        ServicesDiModule.Configure(builder);
        TradosStudioDiModule.Configure(builder);
        FunctionsDiModule.Configure(builder);
    }
}