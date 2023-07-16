using System.Reflection;

namespace Opsi.Pocos
{
    public abstract class InternalWebhookMessageBase : WebhookMessage
    {
        public int FailureCount { get; set; } = default;

        public bool IsDelivered { get; set; } = default;

        public string? LastFailureReason { get; set; }

        public virtual void IncrementFailureCount()
        {
            FailureCount++;
        }

        public virtual WebhookMessage ToWebhookMessage()
        {
            var webhookMessage = new WebhookMessage();

            foreach (var propInfo in webhookMessage.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                propInfo.SetValue(webhookMessage, propInfo.GetValue(this));
            }

            return webhookMessage;
        }
    }
}
