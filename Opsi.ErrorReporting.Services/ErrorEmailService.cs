using Opsi.Common;
using Opsi.Notifications.Abstractions;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorEmailService : IErrorEmailService
{
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ISettingsProvider _settingsProvider;

    public ErrorEmailService(IEmailNotificationService emailNotificationService, ISettingsProvider settingsProvider)
    {
        _emailNotificationService = emailNotificationService;
        _settingsProvider = settingsProvider;
    }

    public async Task SendAsync(Error error)
    {
        const string configSubject = "email.error.subject";
        const string configToAddress = "email.error.toAddress";

        var message = ConvertErrorToEmailMessage(error);
        var subject = _settingsProvider.GetValue(configSubject);
        var toAddress = _settingsProvider.GetValue(configToAddress);

        await _emailNotificationService.SendAsync(subject, message, toAddress);
    }

    private static string ConvertErrorToEmailMessage(Error error)
    {
        return ErrorToString(error);
    }

    private static string ErrorToString(Error error)
    {
        return $@"{ConditionallyRenderOrigin(error)}
{ConditionallyRenderMessage(error)}
{ConditionallyRenderStackTrace(error)}
{ConditionallyRenderInnerException(error)}";
    }

    private static string? ConditionallyRenderInnerException(Error error)
    {
        return error.InnerError != null
            ? $@"{Environment.NewLine}{Environment.NewLine}Inner exception:{Environment.NewLine}{ErrorToString(error.InnerError)}"
            : null;
    }

    private static string? ConditionallyRenderMessage(Error error)
    {
        return !String.IsNullOrWhiteSpace(error.StackTrace)
            ? $@"{Environment.NewLine}{Environment.NewLine}Message:{Environment.NewLine}{error.Message}"
            : null;
    }

    private static string? ConditionallyRenderOrigin(Error error)
    {
        return !String.IsNullOrWhiteSpace(error.StackTrace)
            ? $@"An error occurred in {error.Origin}"
            : null;
    }

    private static string? ConditionallyRenderStackTrace(Error error)
    {
        return !String.IsNullOrWhiteSpace(error.StackTrace)
            ? $@"{Environment.NewLine}{Environment.NewLine}Stack Trace:{Environment.NewLine}{error.StackTrace}"
            : null;
    }
}
