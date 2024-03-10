using Opsi.Common;

namespace Opsi.AzureStorage;

public class StorageServiceBase
{
    private readonly ISettingsProvider _settingsProvider;

    public StorageServiceBase(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;

        StorageConnectionString = new Lazy<string>(GetStorageConnectionString);
    }

    protected Lazy<string> StorageConnectionString { get; }

    private string GetStorageConnectionString()
    {
        const string configConnectionString = "AzureWebJobsStorage";

        var connectionString = _settingsProvider.GetValue(configConnectionString);

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("No connection string configured.");
        }

        return connectionString;
    }
}
