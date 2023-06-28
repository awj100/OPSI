using System;

namespace Opsi.Pocos
{
	public class WebhookMessage
	{
		public WebhookMessage()
		{
			TimeStamp = DateTime.Now;
		}

		public Guid ProjectId { get; set; }

		public string Status { get; set; }

		public DateTime TimeStamp { get; set; }
	}
}
