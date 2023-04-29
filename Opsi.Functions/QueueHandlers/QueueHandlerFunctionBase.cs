using System;
using System.IO;
using System.Threading.Tasks;
using Opsi.Common;
using Opsi.Functions.BaseFunctions;
using Opsi.Functions.Dependencies;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.TradosStudio;

namespace Opsi.Functions;

public abstract class QueueHandlerFunctionBase : FunctionWithStorageProvisions
{
    protected readonly Func<Stream, IPackageService> PackageServiceFactory;

    protected QueueHandlerFunctionBase(Func<Stream, IPackageService> packageServiceFactory, IStorageFunctionDependencies storageFunctionDependencies) : base(storageFunctionDependencies)
    {
        PackageServiceFactory = packageServiceFactory;
    }
}
