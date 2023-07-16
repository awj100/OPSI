namespace Opsi.Pocos
{
    public class WebhookMessage
	{
		public WebhookMessage()
		{
			Id = Guid.NewGuid();
			OccurredOn = DateTime.UtcNow;
        }

		public string Event { get; set; } = default!;

        public Guid Id { get; set; }

		public string Level { get; set; } = default!;

        public DateTime OccurredOn { get; set; }

        public Guid ProjectId { get; set; }

		public string Name { get; set; } = default!;

        public string Username { get; set; } = default!;
    }
}
