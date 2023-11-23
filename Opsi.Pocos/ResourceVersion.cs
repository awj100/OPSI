namespace Opsi.Pocos;

public class ResourceVersion
{
    public virtual string FullName { get; set; } = default!;

    public Guid ProjectId { get; set; }

    public string? Username { get; set; } = default!;

    public string? VersionId { get; set; }

    public int VersionIndex { get; set; }
}
