namespace Opsi.Services.Auth;

internal class AuthService(IAuthHandlerProvider _authHandlerProvider) : IAuthService
{
    public async Task<bool> TrySetAuthenticationContextItems(string? authHeader, IDictionary<object, object> contextItems)
    {
        if (String.IsNullOrWhiteSpace(authHeader))
        {
            return false;
        }

        foreach (var authHandler in _authHandlerProvider.GetAuthHandlers())
        {
            if (await authHandler.AuthenticateAsync(authHeader, contextItems))
            {
                return true;
            }
        }

        return false;
    }
}
