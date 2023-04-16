using System;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Opsi.Services;

internal class SendGridEmailService : IEmailNotificationService
{
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ISendGridClient _sendGridClient;

    public SendGridEmailService(ISendGridClient sendGridClient, IConfiguration configuration)
    {
        const string configFromEmail = "emailNotifications.fromEmail";
        const string configFromName = "emailNotifications.fromName";

        _fromEmail = configuration[configFromEmail];
        _fromName = configuration[configFromName];
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
    }
}
