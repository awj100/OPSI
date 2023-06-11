using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Net.Http.Headers;
using Opsi.Common.Exceptions;

namespace Opsi.Functions.Middleware;

/*
 This is a reference implementation and should not be used for production code.
 The 'Authorization' header is expected to specify a base-64 encoded string in the following format:
    Basic username:claim1,claim2
 If the header is missing or does not contain a correspondingly-formatted string then the request
 will be considered as unauthenticated.
 */
internal class IdentityProvider : IFunctionsWorkerMiddleware
{
    private const string AuthScheme = "Basic";
    private const char HeaderValueSeparator = ':';
    private const string ItemNameAuthHeaderValue = "AuthHeaderValue";
    private const string ItemNameClaims = "Claims";
    private const string ItemNameUsername = "Username";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var authHeader = await GetAuthHeaderValueAsync(context);
        var decodedAuthHeader = DecodeBase64String(authHeader);
        var username = GetUsernameFromDecodedAuthHeader(decodedAuthHeader);
        var claims = GetClaimsFromDecodedAuthHeader(decodedAuthHeader);

        if (String.IsNullOrWhiteSpace(authHeader) || String.IsNullOrWhiteSpace(username) || !claims.Any())
        {
            throw new UnauthenticatedException();
        }

        context.Items.Add(ItemNameAuthHeaderValue, BuildAuthHeaderValue(authHeader));
        context.Items.Add(ItemNameClaims, claims);
        context.Items.Add(ItemNameUsername, username);

        await next(context);
    }

    private static AuthenticationHeaderValue BuildAuthHeaderValue(string? incomingAuthHeaderValue)
    {
        return new AuthenticationHeaderValue(AuthScheme, incomingAuthHeaderValue);
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

    private static async Task<string?> GetAuthHeaderValueAsync(FunctionContext context)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData == null)
        {
            return null;
        }

        if (!requestData.Headers.Contains(HeaderNames.Authorization))
        {
            return null;
        }

        return requestData!.Headers
            .FirstOrDefault(header => header.Key == HeaderNames.Authorization)
            .Value
            .FirstOrDefault()?[AuthScheme.Length..];
    }

    private static IReadOnlyCollection<string> GetClaimsFromDecodedAuthHeader(string? decodedAuthHeader)
    {
        const char claimsSeparator = ',';

        if (String.IsNullOrWhiteSpace(decodedAuthHeader) || !decodedAuthHeader.Contains(HeaderValueSeparator))
        {
            return new List<string>();
        }

        return decodedAuthHeader.Split(HeaderValueSeparator)
                                .ElementAt(1)
                                .Split(claimsSeparator);
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
