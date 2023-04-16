namespace Opsi.Services;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string message, string toAddress);
}