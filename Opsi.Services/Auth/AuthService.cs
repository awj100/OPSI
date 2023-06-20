namespace Opsi.Services.Auth;

internal class AuthService : IAuthService
{
    private readonly IAuthHandlerProvider _authHandlerProvider;

    public AuthService(IAuthHandlerProvider authHandlerProvider)
    {
        _authHandlerProvider = authHandlerProvider;
    }

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
