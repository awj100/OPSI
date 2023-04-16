using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;

namespace Opsi.Services.DiModules;

public static class ServicesDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddScoped<IManifestService>(serviceProvider =>
        {
            return new ManifestService();
        });

        builder.Services.AddSingleton<IEmailNotificationService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var sendGridClient = provider.GetRequiredService<ISendGridClient>();
            return new SendGridEmailService(sendGridClient, configuration);
        });

        builder.Services.AddSingleton<Func<string, IProjectsService>>(provider => connectionString =>
        {
            return new ProjectsService(connectionString);
        });

        builder.Services.AddSingleton<Func<string, string, IQueueService>>(provider => (connectionString, queueName) =>
        {
            return new QueueService(connectionString, queueName);
        });

        builder.Services.AddSingleton<Func<string, IResourcesService>>(provider => connectionString =>
        {
            return new ResourcesService(connectionString);
        });

        builder.Services.AddSingleton<Func<string, IStorageService>>(provider => connectionString =>
        {
            return new StorageService(connectionString);
        });

        builder.Services.AddSendGrid(options =>
        {
            const string configSendGridApiKey = "emailNotifications.sendGridApiKey";
            options.ApiKey = Environment.GetEnvironmentVariable(configSendGridApiKey);
        });
    }
}
