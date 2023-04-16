using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Opsi.Services;

namespace Opsi.Functions.BaseFunctions;

public abstract class FunctionWithConfiguration
{
    protected IConfiguration Configuration { get; private set; }

    protected bool Initialised = false;

    protected void Init(ExecutionContext context)
    {
        if (Initialised)
        {
            return;
        }

        Configuration = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Initialised = true;
    }
}
