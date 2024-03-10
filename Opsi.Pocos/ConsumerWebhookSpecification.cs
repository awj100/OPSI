namespace Opsi.Pocos
{
    public class ConsumerWebhookSpecification
    {
        public ConsumerWebhookSpecification()
        {
            CustomProps = new Dictionary<string, object>(0);
        }

        public ConsumerWebhookSpecification(string uri) : this()
        {
            Uri = uri;
        }

        public ConsumerWebhookSpecification(string uri, Dictionary<string, object> customProps)
        {
            CustomProps = customProps;
            Uri = uri;
        }

        public Dictionary<string, object>? CustomProps { get; set; }

        public string? Uri { get; set; }
    }
}

