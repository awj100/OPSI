namespace Opsi.AzureStorage;

public interface ITableServiceFactory
{
    ITableService Create(string tableName);
}
