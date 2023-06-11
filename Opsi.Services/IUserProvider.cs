using System.Net.Http.Headers;

namespace Opsi.Services;

public interface IUserProvider
{
    Lazy<AuthenticationHeaderValue> AuthHeader { get; }

    Lazy<IReadOnlyCollection<string>> Claims { get; }

    Lazy<string> Username { get; }
}
