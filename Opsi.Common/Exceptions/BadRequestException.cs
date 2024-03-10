namespace Opsi.Common.Exceptions;

public class BadRequestException : ResourceBaseException
{
    public BadRequestException(string failedExpectationMessage) : base(failedExpectationMessage)
    {
    }

    public BadRequestException(Guid projectId, string fullName, string failedExpectationMessage) : base(projectId, fullName, $"Unable to update the resource record: {failedExpectationMessage}")
    {
    }
}
