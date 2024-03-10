namespace Opsi.Services.TableServices;

public interface IOneTimeAuthKeysTableService
{
    Task<bool> AreDetailsValidAsync(string username, string key);

    Task DeleteKeyAsync(string partitionKey, string rowKey);

    Task StoreKeyAsync(OneTimeAuthKeyEntity oneTimeAuthKeyEntity);
}
