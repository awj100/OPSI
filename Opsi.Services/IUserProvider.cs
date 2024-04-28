using System.Net.Http.Headers;

namespace Opsi.Services;

public interface IUserProvider
{
    AuthenticationHeaderValue AuthHeader { get; }

    IReadOnlyCollection<string> Claims { get; }

    bool IsAdministrator { get; }

    string Username { get; }
}
