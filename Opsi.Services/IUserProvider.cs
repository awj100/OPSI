namespace Opsi.Services;

public interface IUserProvider
{
    Task<string?> GetUsernameAsync();
}
