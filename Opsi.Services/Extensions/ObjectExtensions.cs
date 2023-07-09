using System.Reflection;

namespace Opsi.Services.Extensions;

public static class ObjectExtensions
{
    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        return ToDictionary(obj, new HashSet<object>());
    }

    private static Dictionary<string, object?> ToDictionary(object obj, HashSet<object> visited)
    {
        Dictionary<string, object?> dictionary = new();

        if (visited.Contains(obj))
        {
            return dictionary;
        }

        visited.Add(obj);

        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            string propertyName = property.Name;
            object? propertyValue = property.GetValue(obj);

            if (propertyValue != null && !IsSimpleType(propertyValue.GetType()))
            {
                propertyValue = ToDictionary(propertyValue, visited);
            }

            dictionary.Add(propertyName, propertyValue);
        }

        return dictionary;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(Guid);
    }
}
