namespace Opsi.Services.Auth.OneTimeAuth;

internal class OneTimeAuthKeyProvider : IOneTimeAuthKeyProvider
{
    public string GenerateUniqueKey()
    {
        return Guid.NewGuid().ToString();
    }
}
