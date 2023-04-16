using System;

namespace Opsi.Pocos
{
	public class CallbackMessage
	{
		public CallbackMessage()
		{
			Timestamp = DateTime.Now;
		}

		public Guid ProjectId { get; set; }

		public string Status { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
