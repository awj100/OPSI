using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Opsi.Functions.BaseFunctions;
using Opsi.Functions.Dependencies;
using Opsi.Pocos;
using Opsi.AzureStorage;

namespace Opsi.Functions.PackageHandlers;

public class ErrorQueueHandler : FunctionWithConfiguration
{
    private readonly IEmailNotificationService _emailNotificationService;

    public ErrorQueueHandler(IEmailNotificationService emailNotificationService, IStorageFunctionDependencies storageFunctionDependencies) : base()
    {
        _emailNotificationService = emailNotificationService;
    }

    [FunctionName(nameof(ErrorQueueHandler))]
    public async Task Run(
        [QueueTrigger(Constants.QueueNames.Error, Connection = "AzureWebJobsStorage")]Error error,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation($"{nameof(ErrorQueueHandler)} triggered by an exception in {error.Origin}.");

        Init(context);

        try
        {
            var emailBody = GetErrorEmailBody(error);
            var emailRecipient = GetErrorEmailRecipient();
            var emailSubject = GetErrorEmailSubject();

            await _emailNotificationService.SendAsync(emailSubject, emailBody, emailRecipient);
        }
        catch (Exception ex)
        {
            log.LogError(ex, nameof(ErrorQueueHandler));
        }
    }

    private string GetErrorEmailRecipient()
    {
        const string configEmailRecipient = "emailNotifications.errorRecipientEmail";
        return Configuration[configEmailRecipient];
    }

    private string GetErrorEmailSubject()
    {
        const string configEmailSubject = "emailNotifications.errorSubject";
        return Configuration[configEmailSubject];
    }

    private static string GetErrorEmailBody(Error error)
    {
        static void ErrorToHtmlString(StringBuilder sb, Error error)
        {
            sb.Append("<p>An error occurred in ");
            sb.Append(error.Origin);
            sb.AppendLine(".</p>");

            if (!String.IsNullOrWhiteSpace(error.Message))
            {
                sb.Append("<p>");
                sb.Append(error.Message);
                sb.AppendLine("</p>");
            }

            if (!String.IsNullOrWhiteSpace(error.StackTrace))
            {
                sb.AppendLine("<h4>Stack trace:</h4");
                sb.AppendLine(error.StackTrace.Replace("\n", "<br/>"));
            }

            if (error.InnerError != null)
            {
                sb.AppendLine("<h4 style=\"margin-top: 15px;\">Inner error</h4>");
                ErrorToHtmlString(sb, error.InnerError);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("<h2 style=\"color: red;\">Error</h2>");
        ErrorToHtmlString(sb, error);

        return sb.ToString();
    }
}
