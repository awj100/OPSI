using Functions.Worker.ContextAccessor;

namespace Opsi.Services;

internal class UserProvider : IUserProvider
{
    private const string ItemNameUsername = "Username";

    private readonly IErrorQueueService _errorQueueService;
    private readonly IFunctionContextAccessor _functionContextAccessor;

    public UserProvider(IFunctionContextAccessor accessor, IErrorQueueService errorQueueService)
    {
        _functionContextAccessor = accessor;
        _errorQueueService = errorQueueService;
    }

    public async Task<string?> GetUsernameAsync()
    {
        if (!_functionContextAccessor.FunctionContext.Items.ContainsKey(ItemNameUsername))
        {
            await _errorQueueService.ReportAsync(new Exception($"\"{ItemNameUsername}\" has not been configured by the middleware."));
        }

        return _functionContextAccessor.FunctionContext.Items[ItemNameUsername]?.ToString();
    }
}
