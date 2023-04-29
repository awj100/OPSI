namespace Opsi.AzureStorage;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string message, string toAddress);
}