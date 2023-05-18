using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public interface IErrorStorageService
{
    Task StoreAsync(Error error);
}
