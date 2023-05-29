using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Opsi.Constants;
using Opsi.Pocos;

namespace Opsi.Functions;

public class CallbackPostman : FunctionBase
{
    private readonly ILogger _logger;

    public CallbackPostman(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CallbackPostman>();
    }

    [Function(nameof(CallbackPostman))]
    public void Run([QueueTrigger(QueueNames.Callback, Connection = ConfigNameConnectionString)] CallbackMessage callbackMessage)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {callbackMessage.ProjectId} | \"{callbackMessage.Status}\".");
    }
}
