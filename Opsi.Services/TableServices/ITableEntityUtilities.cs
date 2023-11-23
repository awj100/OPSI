using Azure.Data.Tables;

namespace Opsi.Services.TableServices;

public interface ITableEntityUtilities
{
    IReadOnlyCollection<string> GetPropertyNames<TTableEntity>();

    IReadOnlyCollection<string> GetPropertyNames(Type type);

    TTableEntity ParseTableEntityAs<TTableEntity>(TableEntity tableEntity) where TTableEntity : new();

    TTableEntity ParseTableEntityAs<TTableEntity>(TableEntity tableEntity, IReadOnlyCollection<string> ignorablePropertyNames) where TTableEntity : new();

    ITableEntity? ParseTableEntityAsType(Type typeForActivation, TableEntity tableEntity);

    ITableEntity? ParseTableEntityAsType(Type typeForActivation, TableEntity tableEntity, IReadOnlyCollection<string> ignorablePropertyNames);
}