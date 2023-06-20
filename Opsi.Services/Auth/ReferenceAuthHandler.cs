namespace Opsi.Services.Auth;

/*
 This is a reference implementation and should not be used for production code.
 The 'Authorization' header is expected to specify a base-64 encoded string in the following format:
    Basic username:claim1,claim2
 If the header is missing or does not contain a correspondingly-formatted string then the request
 will be considered as unauthenticated.
 */
internal class ReferenceAuthHandler : AuthHandlerBase, IAuthHandler
{
    private const string AuthScheme = "Basic";
    private const char HeaderValueSeparator = ':';

    public Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems)
    {
        if (!authHeader.StartsWith(AuthScheme))
        {
            return Task.FromResult(false);
        }

        var parts = authHeader.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return Task.FromResult(false);
        }

        var encodedAuthParameter = parts[1];
        var decodedAuthHeader = DecodeBase64String(encodedAuthParameter);
        var username = GetUsernameFromDecodedAuthHeader(decodedAuthHeader);
        var claims = GetClaimsFromDecodedAuthHeader(decodedAuthHeader);

        if (String.IsNullOrWhiteSpace(username) || !claims.Any())
        {
            return Task.FromResult(false);
        }

        // In a full implementation the user's credentials would be validated here.

        contextItems.Add(ItemNameClaims, claims);
        contextItems.Add(ItemNameUsername, username);

        return Task.FromResult(true);
    }

    private static string? DecodeBase64String(string? base64EncodedData)
    {
        if (String.IsNullOrWhiteSpace(base64EncodedData))
        {
            return null;
        }

        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private static IReadOnlyCollection<string> GetClaimsFromDecodedAuthHeader(string? decodedAuthHeader)
    {
        const char claimsSeparator = ',';

        if (String.IsNullOrWhiteSpace(decodedAuthHeader) || !decodedAuthHeader.Contains(HeaderValueSeparator))
        {
            return new List<string>();
        }

        var parts = decodedAuthHeader.Split(HeaderValueSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (!parts.Any() || parts.Length < 2)
        {
            return new List<string>();
        }

        return parts.ElementAt(1).Split(claimsSeparator);
    }

    private static string? GetUsernameFromDecodedAuthHeader(string? decodedAuthHeader)
    {
        if (String.IsNullOrWhiteSpace(decodedAuthHeader) || !decodedAuthHeader.Contains(HeaderValueSeparator))
        {
            return null;
        }

        return decodedAuthHeader.Split(HeaderValueSeparator).First();
    }
}
