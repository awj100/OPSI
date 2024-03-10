namespace Opsi.Common.Exceptions;

public class ResourceLockConflictException : ResourceBaseException
{
    public ResourceLockConflictException(Guid projectId, string fullName) : base(projectId, fullName, "Resource is currently locked to another user.")
    {
    }
}
