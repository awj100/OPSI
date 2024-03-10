namespace Opsi.AzureStorage;

internal class TableServiceFactory : ITableServiceFactory
{
    private readonly Func<string, ITableService> _tableServiceFactory;

    public TableServiceFactory(Func<string, ITableService> tableServiceFactory)
    {
        _tableServiceFactory = tableServiceFactory;
    }

    public ITableService Create(string tableName)
    {
        return _tableServiceFactory(tableName);
    }
}
