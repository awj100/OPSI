using Functions.Worker.ContextAccessor;
using Microsoft.Extensions.Hosting;
using Opsi.AzureStorage.DiModules;
using Opsi.ErrorReporting.Services.DiModules;
using Opsi.Functions.DiModules;
using Opsi.Functions.Extensions;
using Opsi.Functions.Middleware;
using Opsi.Notifications.SendGrid.DiModules;
using Opsi.Services.DiModules;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        workerApplication.UseWhenHttpTriggered<HttpResponseExceptionHandling>()
                         .UseWhenNotHttpTriggered<ExceptionHandling>()
                         .UseWhenHttpTriggered<IdentityProvider>()
                         .UseFunctionContextAccessor();
    })
    .ConfigureServices(services =>
    {
        services.AddFunctionContextAccessor();

        FunctionsDiModule.Configure(services);
        AzureStorageDiModule.Configure(services);
        ErrorServicesDiModule.Configure(services);
        SendGridDiModule.Configure(services);
        ServicesDiModule.Configure(services);
    })
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build();

host.Run();
