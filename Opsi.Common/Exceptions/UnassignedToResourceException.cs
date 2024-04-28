namespace Opsi.Common.Exceptions;

public class UnassignedToResourceException : ResourceBaseException
{
    public UnassignedToResourceException() : base("User has not been assigned to the requested resource.")
    {
    }
}
