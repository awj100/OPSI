using System.Text.Json;
using System.Text.Json.Serialization;
using Functions.Worker.ContextAccessor;
using Microsoft.Extensions.DependencyInjection;
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
                         .UseWhenHttpTriggered<AdministratorEnforcement>()
                         .UseFunctionContextAccessor();
    })
    .ConfigureServices(services =>
    {
        services.AddFunctionContextAccessor();

        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
        });

        AzureStorageDiModule.Configure(services);
        ErrorServicesDiModule.Configure(services);
        SendGridDiModule.Configure(services);
        ServicesDiModule.Configure(services);
        FunctionsDiModule.Configure(services);
    })
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build();

host.Run();
