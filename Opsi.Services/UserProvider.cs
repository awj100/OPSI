using System.Net.Http.Headers;
using Functions.Worker.ContextAccessor;
using Opsi.Common.Exceptions;

namespace Opsi.Services;

internal class UserProvider : IUserProvider
{
    private const string ItemNameAuthHeaderValue = "AuthHeaderValue";
    private const string ItemNameClaims = "Claims";
    private const string ItemNameIsAdministrator = "IsAdministrator";
    private const string ItemNameUsername = "Username";

    private readonly IFunctionContextAccessor _functionContextAccessor;

    public UserProvider(IFunctionContextAccessor accessor)
    {
        _functionContextAccessor = accessor;
    }

    public Lazy<AuthenticationHeaderValue> AuthHeader => new(() =>
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameAuthHeaderValue))
        {
            throw new UnauthenticatedException();
        }

        return (AuthenticationHeaderValue)_functionContextAccessor.FunctionContext.Items[ItemNameAuthHeaderValue];
    });

    public Lazy<IReadOnlyCollection<string>> Claims => new(() =>
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameClaims))
        {
            return new List<string>(0);
        }

        return (IReadOnlyCollection<string>)_functionContextAccessor.FunctionContext.Items[ItemNameClaims];
    });

    public Lazy<bool> IsAdministrator => new(() => {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameIsAdministrator))
        {
            return false;
        }

        var isAdministrator = _functionContextAccessor.FunctionContext.Items[ItemNameIsAdministrator];
        if (isAdministrator is not bool)
        {
            throw new Exception($"The FunctionContext's items collection has been populated with an invalid (non-boolean) value for \"{ItemNameIsAdministrator}\".");
        }

        return (bool)isAdministrator;
    });

    public Lazy<string> Username => new(() =>
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            throw new UnauthenticatedException();
        }

        return (string)_functionContextAccessor.FunctionContext.Items[ItemNameUsername];
    });
}
