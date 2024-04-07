using System.Net.Http.Headers;

namespace Opsi.Services.Auth.OneTimeAuth;

public interface IOneTimeAuthService
{
    Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(string username, bool isAdministrator);

    Task<OneTimeAuthCredentials> GetCredentialsAsync(string authenticationHeader);
}
