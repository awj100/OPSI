using System.Net.Http.Headers;
using System.Text;
using Opsi.Services.TableServices;

namespace Opsi.Services.Auth.OneTimeAuth;

internal class OneTimeAuthService(IOneTimeAuthKeyProvider _oneTimeAuthKeyProvider, IOneTimeAuthKeysTableService _oneTimeAuthKeysTableService) : IOneTimeAuthService
{
    private const char AuthParamSeparator = ':';
    private const string AuthScheme = "OneTime";

    public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(string username, bool isAdministrator)
    {
        var key = _oneTimeAuthKeyProvider.GenerateUniqueKey();

        await StoreKeyAsync(username, key);

        var authParameter = $"{username}{AuthParamSeparator}{isAdministrator.ToString().ToLower()}{AuthParamSeparator}{key}";
        var base64EncodedAuthParam = Base64Encode(authParameter);

        return new AuthenticationHeaderValue(AuthScheme, base64EncodedAuthParam);
    }

    public async Task<OneTimeAuthCredentials> GetCredentialsAsync(string authenticationHeader)
    {
        const int expectedPartsCount = 2;
        const int expectedParameterPartsCount = 3;

        if (!authenticationHeader.StartsWith(AuthScheme))
        {
            return new OneTimeAuthCredentials(String.Empty, false, false);
        }

        var parts = authenticationHeader.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < expectedPartsCount)
        {
            return new OneTimeAuthCredentials(String.Empty, false, false);
        }

        var scheme = parts[0];
        var parameter = parts[1];

        if (!scheme.Equals(AuthScheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parameter))
        {
            return new OneTimeAuthCredentials(String.Empty, false, false);
        }

        var decodedValue = Base64Decode(parameter);
        var decodedParts = decodedValue.Split(AuthParamSeparator);
        if (decodedParts.Length != expectedParameterPartsCount)
        {
            return new OneTimeAuthCredentials(String.Empty, false, false);
        }

        var key = decodedParts[2];
        var username = decodedParts[0];

        if (await IsKeyValidAsync(username, key))
        {
            await DeleteKeyAsync(username, key);

            if (!Boolean.TryParse(decodedParts[1], out var isAdministrator))
            {
                return new OneTimeAuthCredentials(String.Empty, false, false);
            }

            return new OneTimeAuthCredentials(username, isAdministrator, true);
        }

        return new OneTimeAuthCredentials(String.Empty, false, false);
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
        /*
            When storing the one-time auth key, don't store the IsAdministrator value
            - IsAdministrator will be passed in the header, but we will us only the username and key for matching/verifying the header.
            Determining whether a user has administrator-level access is determined by the (third-party) implementation-specific authorisation.
            Furthermore, if anyone were to gain access to the storage table, they would not see which users are administrators.
        */

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
