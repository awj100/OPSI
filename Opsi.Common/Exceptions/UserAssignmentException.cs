namespace Opsi.Common.Exceptions;

public class UserAssignmentException : ResourceBaseException
{
    public UserAssignmentException(Guid projectId, string resourceFullName, string message) : base(projectId, resourceFullName, message)
    {
    }
}
