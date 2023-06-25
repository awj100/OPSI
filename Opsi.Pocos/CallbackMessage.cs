using System;

namespace Opsi.Pocos
{
	public class CallbackMessage
	{
		public CallbackMessage()
		{
			TimeStamp = DateTime.Now;
		}

		public Guid ProjectId { get; set; }

		public string Status { get; set; }

		public DateTime TimeStamp { get; set; }
	}
}
