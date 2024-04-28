namespace Opsi.Common.Exceptions;

public class InvalidProjectStateException : Exception
{
    public InvalidProjectStateException(string invalidState) : base($"Invalid state: \"{invalidState}\".")
    {
    }
}
