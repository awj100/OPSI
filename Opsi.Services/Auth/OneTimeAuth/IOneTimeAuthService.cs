using System.Net.Http.Headers;

namespace Opsi.Services.Auth.OneTimeAuth;

public interface IOneTimeAuthService
{
    Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(string username, Guid projectId, string filePath);

    Task<string?> GetUsernameAsync(string authenticationHeader);
}
