using Microsoft.Extensions.Logging;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorService : IErrorService
{
    private readonly IErrorEmailService _emailService;
    private readonly ILogger<ErrorService> _log;
    private readonly IErrorStorageService _storageService;

    public ErrorService(IErrorEmailService emailService,
                        IErrorStorageService storageService,
                        ILoggerFactory loggerFactory)
    {
        _emailService = emailService;
        _log = loggerFactory.CreateLogger<ErrorService>();
        _storageService = storageService;
    }

    public async Task ReportAsync(Error error)
    {
        try
        {
            await _emailService.SendAsync(error);
        }
        catch(Exception exception)
        {
            _log.LogCritical(exception, "UNABLE TO SEND ERROR EMAIL!");
        }

        try
        {
            await _storageService.StoreAsync(error);
        }
        catch (Exception exception)
        {
            _log.LogCritical(exception, "UNABLE TO STORE ERROR!");
        }
    }
}
