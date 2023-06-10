namespace Opsi.Services;

public interface IUserProvider
{
    Lazy<IReadOnlyCollection<string>> Claims { get; }

    Lazy<string> Username { get; }
}
