namespace Opsi.Notifications.Abstractions;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string message, string toAddress);
}