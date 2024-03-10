using Opsi.Services.Auth.OneTimeAuth;

namespace Opsi.Services.Auth;

internal class OneTimeKeyAuthHandler : AuthHandlerBase, IAuthHandler
{
    private readonly IOneTimeAuthService _oneTimeAuthService;

    public OneTimeKeyAuthHandler(IOneTimeAuthService oneTimeAuthService)
    {
        _oneTimeAuthService = oneTimeAuthService;
    }

    public async Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems)
    {
        var username = await _oneTimeAuthService.GetUsernameAsync(authHeader);

        if (String.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        contextItems.Add(ItemNameUsername, username);

        return true;
    }
}
