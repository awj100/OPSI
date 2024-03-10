namespace Opsi.Common.Exceptions;

public class ResourceLockException : ResourceBaseException
{
    public ResourceLockException(Guid projectId, string fullName) : base(projectId, fullName, "Unable to update the resource record.")
    {
    }

    public ResourceLockException(Guid projectId, string fullName, string errorMessage) : base(projectId, fullName, $"Unable to update the resource record: {errorMessage}")
    {
    }
}
