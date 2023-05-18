using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public interface IErrorService
{
    Task ReportAsync(Error error);
}
