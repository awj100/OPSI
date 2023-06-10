using Functions.Worker.ContextAccessor;
using Opsi.Common.Exceptions;

namespace Opsi.Services;

internal class UserProvider : IUserProvider
{
    private const string ItemNameClaims = "Claims";
    private const string ItemNameUsername = "Username";

    private readonly IFunctionContextAccessor _functionContextAccessor;

    public UserProvider(IFunctionContextAccessor accessor)
    {
        _functionContextAccessor = accessor;
    }

    public Lazy<IReadOnlyCollection<string>> Claims => new(() =>
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            return new List<string>(0);
        }

        return (List<string>)_functionContextAccessor.FunctionContext.Items[ItemNameClaims];
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
