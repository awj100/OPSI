namespace Opsi.Pocos
{
    public class InternalWebhookMessage : InternalWebhookMessageBase
    {
        public InternalWebhookMessage()
        {
        }

        public InternalWebhookMessage(WebhookMessage webhookMessage, ConsumerWebhookSpecification webhookSpec)
        {
            foreach (var propInfo in webhookMessage.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                propInfo.SetValue(this, propInfo.GetValue(webhookMessage));
            }

            WebhookSpecification = webhookSpec;
        }

        public ConsumerWebhookSpecification WebhookSpecification { get; set; }
    }
}
