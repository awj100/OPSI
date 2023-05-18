using Opsi.AzureStorage;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorStorageService : TableServiceBase, IErrorStorageService
{
    private const string TableName = "errors";

    public ErrorStorageService(ISettingsProvider settingsProvider) : base(settingsProvider, TableName)
    {
    }

    public async Task StoreAsync(Error error)
    {
        var errorTableEntity = new ErrorTableEntity(error);

        await StoreTableEntityAsync(errorTableEntity);
    }
}
