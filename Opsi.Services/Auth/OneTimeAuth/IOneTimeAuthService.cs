using System.Net.Http.Headers;

namespace Opsi.Services.Auth.OneTimeAuth;

public interface IOneTimeAuthService
{
    Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(string username);

    Task<string?> GetUsernameAsync(string authenticationHeader);
}
