namespace Opsi.Common.Exceptions;

public abstract class ResourceBaseException : Exception
{
    public ResourceBaseException(string message) : base(message)
    {
    }

    public ResourceBaseException(Guid projectId, string fullName, string message) : base(message)
    {
        FullName = fullName;
        ProjectId = projectId;
    }

    public string? FullName { get; set; }

    public Guid? ProjectId { get; set; }
}
