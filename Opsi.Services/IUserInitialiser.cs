namespace Opsi.Services;

public interface IUserInitialiser
{
    void SetUsername(string username, bool isAdministrator);
}
