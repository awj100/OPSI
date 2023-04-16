using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Opsi.Functions.BaseFunctions;
using Opsi.Pocos;

namespace Opsi.Functions.PackageHandlers;

public class CallbackPostman : FunctionWithConfiguration
{
    [FunctionName(nameof(CallbackPostman))]
    public async Task Run(
        [QueueTrigger("callback-messages", Connection = "AzureWebJobsStorage")]CallbackMessage callbackMessage,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation($"C# Queue trigger function processed: {callbackMessage.ProjectId} | \"{callbackMessage.Status}\".");

        Init(context);
    }
}
