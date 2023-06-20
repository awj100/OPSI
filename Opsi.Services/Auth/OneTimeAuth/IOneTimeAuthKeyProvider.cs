namespace Opsi.Services.Auth.OneTimeAuth;

public interface IOneTimeAuthKeyProvider
{
    string GenerateUniqueKey();
}
