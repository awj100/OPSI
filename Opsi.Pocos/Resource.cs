namespace Opsi.Pocos;

public class Resource
{
    public virtual string FullName { get; set; } = default!;

    public string? LockedTo { get; set; } = default;

    public string? Username { get; set; } = default!;

    public string? VersionId { get; set; }

    public int VersionIndex { get; set; }
}
