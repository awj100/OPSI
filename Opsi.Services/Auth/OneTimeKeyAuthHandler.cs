using Opsi.Services.Auth.OneTimeAuth;

namespace Opsi.Services.Auth;

internal class OneTimeKeyAuthHandler(IOneTimeAuthService _oneTimeAuthService) : AuthHandlerBase, IAuthHandler
{
    public async Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems)
    {
        var credentials = await _oneTimeAuthService.GetCredentialsAsync(authHeader);

        if (!credentials.IsValid)
        {
            return false;
        }

        contextItems.Add(ItemNameIsAdministrator, credentials.IsAdministrator);
        contextItems.Add(ItemNameUsername, credentials.Username);

        return true;
    }
}
