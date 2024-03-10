namespace Opsi.Common;

public class SettingsProvider : ISettingsProvider
{
    public string GetValue(string name,
                           bool canBeNullOrEmpty = false,
                           [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
    {
        var val = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        if (!canBeNullOrEmpty && String.IsNullOrWhiteSpace(val))
        {
            throw new Exception($"{callerName}: Missing configuration property (\"{name}\").");
        }

#pragma warning disable CS8603 // Possible null reference return.
        return val;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public T GetValue<T>(string name,
                         bool canBeNullOrEmpty = false,
                         [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
    {
        var val = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        if (!canBeNullOrEmpty && String.IsNullOrWhiteSpace(val))
        {
            throw new Exception($"{callerName}: Missing configuration property (\"{name}\").");
        }

        try
        {
            if (String.IsNullOrWhiteSpace(val))
            {
#pragma warning disable CS8603 // Possible null reference return.
                return default;
#pragma warning restore CS8603 // Possible null reference return.
            }

            return (T)Convert.ChangeType(val, typeof(T));
        }
        catch (Exception)
        {
            throw new Exception($"Unable to cast value of \"{name}\" to {typeof(T).Name}.");
        }
    }
}