namespace Opsi.Services.Auth;

public interface IAuthHandlerProvider
{
    IReadOnlyCollection<IAuthHandler> GetAuthHandlers();
}