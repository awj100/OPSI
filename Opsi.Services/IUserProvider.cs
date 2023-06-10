namespace Opsi.Services;

public interface IUserProvider
{
    IReadOnlyCollection<string>? GetClaims();

    string? GetUsername();
}
