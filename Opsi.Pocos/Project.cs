namespace Opsi.Pocos
{
    public class Project : ProjectBase
    {
        public Project()
        {
        }

        public Project(InternalManifest internalManifest, string projectState)
        {
            Id = internalManifest.ProjectId;
            Name = internalManifest.PackageName;
            State = projectState;
            Username = internalManifest.Username;
            WebhookSpecification = internalManifest.WebhookSpecification;
        }

        public ConsumerWebhookSpecification WebhookSpecification { get; set; }
    }
}
