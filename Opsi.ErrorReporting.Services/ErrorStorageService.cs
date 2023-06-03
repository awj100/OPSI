using Opsi.AzureStorage;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services;

public class ErrorStorageService : IErrorStorageService
{
    private const string TableName = "errors";
    private ITableService _tableService;

    public ErrorStorageService(ITableServiceFactory tableServiceFactory)
    {
        _tableService = tableServiceFactory.Create(TableName);
    }

    public async Task StoreAsync(Error error)
    {
        var errorTableEntity = new ErrorTableEntity(error);

        await _tableService.StoreTableEntityAsync(errorTableEntity);

        string? parentRowKey = errorTableEntity.RowKey;
        Error innerError = error.InnerError;

        while (innerError != null)
        {
            var innerErrorTableEntity = new ErrorTableEntity(innerError) { ParentRowKey = parentRowKey };

            await _tableService.StoreTableEntityAsync(innerErrorTableEntity);

            innerError = innerError.InnerError;
            parentRowKey = innerErrorTableEntity.RowKey;
        }
    }
}
