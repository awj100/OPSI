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

		public Guid Id { get; set; }

		public DateTime OccurredOn { get; set; }

        public Guid ProjectId { get; set; }

		public string Status { get; set; }

		public string Username { get; set; }
	}
}
