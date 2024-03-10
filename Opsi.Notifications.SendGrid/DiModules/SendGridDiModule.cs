using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Notifications.Abstractions;
using SendGrid.Extensions.DependencyInjection;

namespace Opsi.Notifications.SendGrid.DiModules;

public static class SendGridDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        Configure(builder.Services);
    }

    public static void Configure(IServiceCollection services)
    {
        services
            .AddSingleton<IEmailNotificationService, SendGridEmailService>()
            .AddSendGrid(options =>
            {
                const string configSendGridApiKey = "emailNotifications.sendGridApiKey";
                options.ApiKey = Environment.GetEnvironmentVariable(configSendGridApiKey);
            });
    }
}
