using System.Net.Http.Headers;

namespace Opsi.Services;

public interface IUserProvider
{
    Lazy<AuthenticationHeaderValue> AuthHeader { get; }

    Lazy<IReadOnlyCollection<string>> Claims { get; }

    Lazy<bool> IsAdministrator { get; }

    Lazy<string> Username { get; }
}
