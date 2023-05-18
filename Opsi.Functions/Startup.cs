using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Opsi.AzureStorage.DiModules;
using Opsi.Common.DiModules;
using Opsi.ErrorReporting.Services.DiModules;
using Opsi.Notifications.SendGrid.DiModules;
using Opsi.Services.DiModules;

[assembly: FunctionsStartup(typeof(Opsi.Functions.Startup))]

namespace Opsi.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        AzureStorageDiModule.Configure(builder);
        ErrorServicesDiModule.Configure(builder);
        SendGridDiModule.Configure(builder);
        ServicesDiModule.Configure(builder);
    }
}