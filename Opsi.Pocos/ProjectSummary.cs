namespace Opsi.Pocos;

public class ProjectSummary
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string State { get; set; } = default!;

    public override string ToString()
    {
        return $"{(String.IsNullOrWhiteSpace(Name) ? "[No name]" : Name)} ({Id} | {State})";
    }
}
