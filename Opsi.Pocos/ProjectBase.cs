namespace Opsi.Pocos
{
    public abstract class ProjectBase
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? State { get; set; }

        public string? Username { get; set; }

        public override string ToString()
        {
            return $"{(String.IsNullOrWhiteSpace(Name) ? "[No name]" : Name)} ({Id})";
        }
    }
}
