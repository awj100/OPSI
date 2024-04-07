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

    public Lazy<AuthenticationHeaderValue> AuthHeader => new(() =>
    {
        if (!_functionContext.Items.ContainsKey(ItemNameAuthHeaderValue))
        {
            throw new UnauthenticatedException();
        }

        return (AuthenticationHeaderValue)_functionContext.Items[ItemNameAuthHeaderValue];
    });

    public Lazy<IReadOnlyCollection<string>> Claims => new(() =>
    {
        if (!_functionContext.Items.ContainsKey(ItemNameClaims))
        {
            return new List<string>(0);
        }

        return (IReadOnlyCollection<string>)_functionContext.Items[ItemNameClaims];
    });

    public Lazy<bool> IsAdministrator => new(() => {
        if (!_functionContext.Items.ContainsKey(ItemNameIsAdministrator))
        {
            return false;
        }

        var isAdministrator = _functionContext.Items[ItemNameIsAdministrator];
        if (isAdministrator is not bool)
        {
            throw new Exception($"The FunctionContext's items collection has been populated with an invalid (non-boolean) value for \"{ItemNameIsAdministrator}\".");
        }

        return (bool)isAdministrator;
    });

    public Lazy<string> Username => new(() =>
    {
        if (!_functionContext.Items.ContainsKey(ItemNameUsername))
        {
            throw new UnauthenticatedException();
        }

        return (string)_functionContext.Items[ItemNameUsername];
    });

    public void SetUsername(string username, bool isAdministrator)
    {
        _functionContext.Items[ItemNameIsAdministrator] = isAdministrator;
        _functionContext.Items[ItemNameUsername] = username;
    }
}
