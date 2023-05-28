using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opsi.Notifications.Abstractions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Opsi.Notifications.SendGrid;

internal class SendGridEmailService : IEmailNotificationService
{
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISendGridClient _sendGridClient;

    public SendGridEmailService(ISendGridClient sendGridClient,
                                IConfiguration configuration,
                                ILoggerFactory loggerFactory)
    {
        const string configFromEmail = "emailNotifications.fromEmail";
        const string configFromName = "emailNotifications.fromName";

        _fromEmail = configuration[configFromEmail];
        _fromName = configuration[configFromName];
        _loggerFactory = loggerFactory;
        _sendGridClient = sendGridClient;
    }

    public async Task SendAsync(string subject, string message, string toAddress)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = subject
        };
        msg.AddContent(MimeType.Html, message);
        msg.AddTo(new EmailAddress(toAddress));
        var response = await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var log = _loggerFactory.CreateLogger<SendGridEmailService>();
        log.LogCritical($"Unable to send email notification via SendGrid. The response was {response.StatusCode}.");
    }
}
