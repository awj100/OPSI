using Functions.Worker.ContextAccessor;

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

    public IReadOnlyCollection<string>? GetClaims()
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            return new List<string>(0);
        }

        return (List<string>)_functionContextAccessor.FunctionContext.Items[ItemNameClaims];
    }

    public string? GetUsername()
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            return null;
        }

        return (string)_functionContextAccessor.FunctionContext.Items[ItemNameUsername];
    }
}
