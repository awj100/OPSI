namespace Opsi.Common.Exceptions;

public class ResourceNotFoundException : ResourceBaseException
{
    public ResourceNotFoundException(Guid projectId, string fullName) : base(projectId, fullName, "The specified resource could not be found.")
    {
    }
}
