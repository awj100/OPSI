using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Opsi.Functions.Dependencies;

namespace Opsi.Functions.DiModules;

public static class FunctionsDiModule
{
    public static void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<IStorageFunctionDependencies, StorageFunctionDependencies>();
    }
}
