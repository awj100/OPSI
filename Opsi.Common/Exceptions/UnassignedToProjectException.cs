namespace Opsi.Common.Exceptions;

public class UnassignedToProjectException : ResourceBaseException
{
    public UnassignedToProjectException() : base("User has not been assigned to the requested project.")
    {
    }
}
