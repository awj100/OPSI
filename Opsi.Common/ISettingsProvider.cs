using System.Runtime.CompilerServices;

namespace Opsi.Common;

public interface ISettingsProvider
{
    string GetValue(string name, bool canBeNullOrEmpty = false, [CallerMemberName] string callerName = "");

    T GetValue<T>(string name, bool canBeNullOrEmpty = false, [CallerMemberName] string callerName = "");
}