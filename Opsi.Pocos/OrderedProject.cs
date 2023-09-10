namespace Opsi.Pocos;

/// <summary>
/// This class is used for storing projects by ordered keys. It is intended to provide only basic information.
/// </summary>
public class OrderedProject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public override string ToString()
    {
        return $"{(String.IsNullOrWhiteSpace(Name) ? "[No name]" : Name)} ({Id})";
    }
}
