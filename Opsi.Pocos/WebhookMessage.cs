using System;

namespace Opsi.Pocos
{
	public class WebhookMessage
	{
		public WebhookMessage()
		{
			Id = Guid.NewGuid();
			OccurredOn = DateTime.UtcNow;
        }

		public string Event { get; set; }

		public Guid Id { get; set; }

		public string Level { get; set; }

		public DateTime OccurredOn { get; set; }

        public Guid ProjectId { get; set; }

		public string Name { get; set; }

		public string Username { get; set; }
    }
}
