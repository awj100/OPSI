using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public interface IErrorEmailService
{
    Task SendAsync(Error error);
}
