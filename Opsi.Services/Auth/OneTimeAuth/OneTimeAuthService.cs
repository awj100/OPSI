using System.Net.Http.Headers;
using System.Text;
using Opsi.Services.TableServices;

namespace Opsi.Services.Auth.OneTimeAuth;

internal class OneTimeAuthService : IOneTimeAuthService
{
    private const char AuthParamSeparator = ':';
    private const string AuthScheme = "OneTime";
    private readonly IOneTimeAuthKeyProvider _oneTimeAuthKeyProvider;
    private readonly IOneTimeAuthKeysTableService _oneTimeAuthKeysTableService;

    public OneTimeAuthService(IOneTimeAuthKeyProvider oneTimeAuthKeyProvider, IOneTimeAuthKeysTableService oneTimeAuthKeysTableService)
    {
        _oneTimeAuthKeyProvider = oneTimeAuthKeyProvider;
        _oneTimeAuthKeysTableService = oneTimeAuthKeysTableService;
    }

    public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(string username)
    {
        var key = _oneTimeAuthKeyProvider.GenerateUniqueKey();

        await StoreKeyAsync(username, key);

        var authParameter = $"{username}{AuthParamSeparator}{key}";
        var base64EncodedAuthParam = Base64Encode(authParameter);

        return new AuthenticationHeaderValue(AuthScheme, base64EncodedAuthParam);
    }

    public async Task<string?> GetUsernameAsync(string authenticationHeader)
    {
        const int expectedPartsCount = 2;
        const int expectedParameterPartsCount = expectedPartsCount;

        if (!authenticationHeader.StartsWith(AuthScheme))
        {
            return null;
        }

        var parts = authenticationHeader.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < expectedPartsCount)
        {
            return null;
        }

        var scheme = parts[0];
        var parameter = parts[1];

        if (!scheme.Equals(AuthScheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parameter))
        {
            return null;
        }

        var decodedValue = Base64Decode(parameter);
        var decodedParts = decodedValue.Split(AuthParamSeparator);
        if (decodedParts.Length != expectedParameterPartsCount)
        {
            return null;
        }

        var key = decodedParts[1];
        var username = decodedParts[0];

        if (await IsKeyValidAsync(username, key))
        {
            await DeleteKeyAsync(username, key);

            return username;
        }

        return null;
    }

    private async Task DeleteKeyAsync(string username, string key)
    {
        await _oneTimeAuthKeysTableService.DeleteKeyAsync(username, key);
    }

    private async Task<bool> IsKeyValidAsync(string username, string key)
    {
        return await _oneTimeAuthKeysTableService.AreDetailsValidAsync(username, key);
    }

    private async Task StoreKeyAsync(string username, string key)
    {
        var oneTimeAuthKeyEntity = new OneTimeAuthKeyEntity(username, key);

        await _oneTimeAuthKeysTableService.StoreKeyAsync(oneTimeAuthKeyEntity);
    }

    private static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }
}
