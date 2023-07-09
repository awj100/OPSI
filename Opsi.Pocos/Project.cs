namespace Opsi.Pocos
{
    public class Project : ProjectBase
    {
        public Project()
        {
        }

        public Project(InternalManifest internalManifest)
        {
            Id = internalManifest.ProjectId;
            Name = internalManifest.PackageName;
            Username = internalManifest.Username;
            WebhookSpecification = internalManifest.WebhookSpecification;
        }

        public ConsumerWebhookSpecification WebhookSpecification { get; set; }
    }
}
