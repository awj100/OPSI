namespace Opsi.Services.Auth;

public interface IAuthService
{
    Task<bool> TrySetAuthenticationContextItems(string? authHeader, IDictionary<object, object> contextItems);
}