namespace Opsi.Pocos;

public class Resource
{
    public virtual string FullName { get; set; } = default!;

    public string? AssignedBy { get; set; }

    public DateTime? AssignedOnUtc { get; set; }

    public string? AssignedTo { get; set; }

    public Guid ProjectId { get; set; }

    public string? Username { get; set; } = default!;

    public List<ResourceVersion> ResourceVersions { get; set; }

    public Resource()
    {
        ResourceVersions = new List<ResourceVersion>();
    }
}
