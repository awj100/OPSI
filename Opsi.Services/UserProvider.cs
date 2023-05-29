using Functions.Worker.ContextAccessor;

namespace Opsi.Services;

internal class UserProvider : IUserProvider
{
    private const string ItemNameUsername = "Username";

    private readonly IFunctionContextAccessor _functionContextAccessor;

    public UserProvider(IFunctionContextAccessor accessor)
    {
        _functionContextAccessor = accessor;
    }

    public string? GetUsername()
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            return null;
        }

        return _functionContextAccessor.FunctionContext.Items[ItemNameUsername]?.ToString();
    }
}
