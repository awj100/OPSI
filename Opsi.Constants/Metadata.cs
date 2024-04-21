namespace Opsi.Constants;

public static class Metadata
{
    // The value of each string is forced to lower-case because Azure Storage does the same when setting metadata.

    public static readonly string AssignedBy = nameof(AssignedBy).ToLower();
    public static readonly string AssignedOnUtc = nameof(AssignedOnUtc).ToLower();
    public static readonly string Assignee = nameof(Assignee).ToLower();
    public static readonly string CreatedBy = nameof(CreatedBy).ToLower();
    public static readonly string ProjectId = nameof(ProjectId).ToLower();
    public static readonly string ProjectName = nameof(ProjectName).ToLower();
    public static readonly string WebhookCustomProps = nameof(WebhookCustomProps).ToLower();
    public static readonly string WebhookUri = nameof(WebhookUri).ToLower();
}