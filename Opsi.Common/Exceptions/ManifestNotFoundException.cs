namespace Opsi.Common.Exceptions;

public class ManifestNotFoundException : Exception
{
    public ManifestNotFoundException() : base("The specified manifest could not be found.")
    {
    }

    public ManifestNotFoundException(Guid projectId) : base($"No manifest could be found for project with ID \"{projectId}\".")
    {
    }
}
