namespace Opsi.Services.Auth;

public interface IAuthHandler
{
    Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems);
}
