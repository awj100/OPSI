namespace Opsi.Common.Exceptions;

public class ProjectNotFoundException : Exception
{
    public ProjectNotFoundException() : base("The specified project could not be found.")
    {
    }
}
