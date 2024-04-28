using System.Net.Http.Headers;
using Functions.Worker.ContextAccessor;
using Microsoft.Azure.Functions.Worker;
using Opsi.Common.Exceptions;

namespace Opsi.Services;

internal class UserProvider : IUserProvider, IUserInitialiser
{
    private const string ItemNameAuthHeaderValue = "AuthHeaderValue";
    private const string ItemNameClaims = "Claims";
    private const string ItemNameIsAdministrator = "IsAdministrator";
    private const string ItemNameUsername = "Username";

    private readonly FunctionContext _functionContext;

    public UserProvider(IFunctionContextAccessor accessor) : this(accessor.FunctionContext)
    {
    }

    public UserProvider(FunctionContext functionContext)
    {
        _functionContext = functionContext;
    }

    public AuthenticationHeaderValue AuthHeader
    {
        get
        {
            if (!_functionContext.Items.TryGetValue(ItemNameAuthHeaderValue, out object? objAuthHeader))
            {
                throw new UnauthenticatedException();
            }

            if (objAuthHeader is not AuthenticationHeaderValue authHeader)
            {
                throw new Exception($"The FunctionContext's items collection has been populated with an invalid value for \"{ItemNameAuthHeaderValue}\".");
            }

            return authHeader;
        }
    }

    public IReadOnlyCollection<string> Claims
    {
        get
        {
            if (!_functionContext.Items.TryGetValue(ItemNameClaims, out object? objClaims))
            {
                return [];
            }

            if (objClaims is not IReadOnlyCollection<string> claims)
            {
                throw new Exception($"The FunctionContext's items collection has been populated with an invalid value for \"{ItemNameClaims}\".");
            }

            return claims;
        }
    }

    public bool IsAdministrator
    {
        get
        {
            if (!_functionContext.Items.TryGetValue(ItemNameIsAdministrator, out object? objIsAdministrator))
            {
                return false;
            }

            if (objIsAdministrator is not bool isAdministrator)
            {
                throw new Exception($"The FunctionContext's items collection has been populated with an invalid (non-boolean) value for \"{ItemNameIsAdministrator}\".");
            }

            return isAdministrator;
        }
    }

    public string Username
    {
        get
        {
            if (!_functionContext.Items.TryGetValue(ItemNameUsername, out object? username))
            {
                throw new UnauthenticatedException();
            }

            return (string)username;
        }
    }

    public void SetUsername(string username, bool isAdministrator)
    {
        _functionContext.Items[ItemNameIsAdministrator] = isAdministrator;
        _functionContext.Items[ItemNameUsername] = username;
    }
}
