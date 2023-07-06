namespace Opsi.Common.Exceptions;

public class UnauthenticatedException : ResourceBaseException
{
    public UnauthenticatedException() : base("User has not been authenticated.")
    {
    }
}
