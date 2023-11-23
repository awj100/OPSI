namespace Opsi.Pocos;

public class UserAssignment
{
    public string AssignedByUsername { get; set; } = default!;

    public DateTime AssignedOnUtc { get; set; }

    public string AssigneeUsername { get; set; } = default!;

    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = default!;

    public virtual string ResourceFullName { get; set; } = default!;

    public override string ToString()
    {
        return $"{ProjectId} | {AssigneeUsername} | {ResourceFullName}";
    }
}
