using Azure.Data.Tables;
using System.Reflection;

namespace Opsi.Services.TableServices;

internal class TableEntityUtilities : ITableEntityUtilities
{
    public IReadOnlyCollection<string> GetPropertyNames<TTableEntity>()
    {
        return GetProperties<TTableEntity>().Select(propInfo => propInfo.Name)
                                            .ToList();
    }

    public TTableEntity ParseTableEntityAs<TTableEntity>(TableEntity tableEntity) where TTableEntity : new()
    {
        return ParseTableEntityAs<TTableEntity>(tableEntity, new List<string>(0));
    }

    public TTableEntity ParseTableEntityAs<TTableEntity>(TableEntity tableEntity, IReadOnlyCollection<string> ignorablePropertyNames) where TTableEntity : new()
    {
        var instance = new TTableEntity();

        foreach (var propInfo in GetProperties<TTableEntity>().Where(pi => !ignorablePropertyNames.Contains(pi.Name))
                                                              .ToList())
        {
            var propertyType = propInfo.PropertyType == typeof(Nullable)
                ? Nullable.GetUnderlyingType(propInfo.PropertyType)
                : propInfo.PropertyType;

            if (propertyType == typeof(byte[]))
            {
                propInfo.SetValue(instance, tableEntity.GetBinary(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(bool))
            {
                propInfo.SetValue(instance, tableEntity.GetBoolean(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(DateTime))
            {
                propInfo.SetValue(instance, tableEntity.GetDateTime(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(DateTimeOffset))
            {
                propInfo.SetValue(instance, tableEntity.GetDateTimeOffset(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(double))
            {
                propInfo.SetValue(instance, tableEntity.GetDouble(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(Guid))
            {
                propInfo.SetValue(instance, tableEntity.GetGuid(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(int))
            {
                propInfo.SetValue(instance, tableEntity.GetInt32(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(long))
            {
                propInfo.SetValue(instance, tableEntity.GetInt64(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(string))
            {
                propInfo.SetValue(instance, tableEntity.GetString(propInfo.Name));
                continue;
            }

            if (tableEntity.TryGetValue(propInfo.Name, out var val))
            {
                try
                {
                    propInfo.SetValue(instance, val);
                    continue;
                }
                catch (Exception)
                {
                }
            }

            throw new InvalidOperationException($"Cannot parse property '{propInfo.Name}' from the TableEntity property bag.");
        }

        return instance;
    }

    public ITableEntity? ParseTableEntityAsType(Type typeForActivation, TableEntity tableEntity)
    {
        return ParseTableEntityAsType(typeForActivation, tableEntity, new List<string>(0));
    }

    public ITableEntity? ParseTableEntityAsType(Type typeForActivation, TableEntity tableEntity, IReadOnlyCollection<string> ignorablePropertyNames)
    {
        if (!typeForActivation.IsAssignableTo(typeof(ITableEntity)))
        {
            throw new ArgumentException($"The type for activation must be assignable to {typeof(ITableEntity).Name}.", nameof(typeForActivation));
        }

        object? instance;
        try
        {
            instance = Activator.CreateInstance(typeForActivation);
        }
        catch (Exception)
        {
            throw new Exception($"Cannot create instance of {typeForActivation.Name}. To create an instance from a storage table entity the type must have a parameterless constructor.");
        }

        foreach (var propInfo in typeForActivation.GetProperties()
                                                  .Where(pi => !ignorablePropertyNames.Contains(pi.Name))
                                                  .ToList())
        {
            var propertyType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

            if (propertyType == typeof(byte[]))
            {
                propInfo.SetValue(instance, tableEntity.GetBinary(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(bool))
            {
                propInfo.SetValue(instance, tableEntity.GetBoolean(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(DateTime))
            {
                propInfo.SetValue(instance, tableEntity.GetDateTime(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(DateTimeOffset))
            {
                propInfo.SetValue(instance, tableEntity.GetDateTimeOffset(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(double))
            {
                propInfo.SetValue(instance, tableEntity.GetDouble(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(Guid))
            {
                propInfo.SetValue(instance, tableEntity.GetGuid(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(int))
            {
                propInfo.SetValue(instance, tableEntity.GetInt32(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(long))
            {
                propInfo.SetValue(instance, tableEntity.GetInt64(propInfo.Name));
                continue;
            }

            if (propertyType == typeof(string))
            {
                propInfo.SetValue(instance, tableEntity.GetString(propInfo.Name));
                continue;
            }

            if (tableEntity.TryGetValue(propInfo.Name, out var val))
            {
                try
                {
                    propInfo.SetValue(instance, val);
                    continue;
                }
                catch (Exception)
                {
                }
            }

            throw new InvalidOperationException($"Cannot parse property '{propInfo.Name}' from the TableEntity property bag.");
        }

        return instance as ITableEntity;
    }

    private static IReadOnlyCollection<PropertyInfo> GetProperties<TTableEntity>()
    {
        return typeof(TTableEntity).GetProperties()
                                   .ToList();
    }
}
