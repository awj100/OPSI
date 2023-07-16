namespace Opsi.Pocos
{
    public abstract class ProjectBase
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;

        public string State { get; set; } = default!;

        public string Username { get; set; } = default!;

        public override string ToString()
        {
            return $"{(String.IsNullOrWhiteSpace(Name) ? "[No name]" : Name)} ({Id})";
        }
    }
}
