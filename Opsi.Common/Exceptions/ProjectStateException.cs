namespace Opsi.Common.Exceptions;

public class ProjectStateException : Exception
{
    public ProjectStateException() : base("The project is in the wrong state for your retrieval.")
    {
    }
}
