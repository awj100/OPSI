namespace Opsi.Pocos
{
    public class InternalManifest : Manifest
	{
        // Required for deserialisation.
        public InternalManifest()
        {
            PackageName = String.Empty;
            ProjectId = Guid.Empty;
            ResourceExclusionPaths = new List<string>();
        }

        public InternalManifest(Manifest manifest, string username)
		{
            foreach (var propertyInfo in manifest.GetType()
                                                 .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                                                 .Where(propInfo => propInfo.Name != nameof(Manifest.WebhookSpecification)))
            {
                propertyInfo.SetValue(this, propertyInfo.GetValue(manifest));
            }

            if (manifest.WebhookSpecification != null)
            {
                WebhookSpecification = new ConsumerWebhookSpecification();
                foreach (var propertyInfo in manifest.WebhookSpecification.GetType()
                                                                          .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    propertyInfo.SetValue(WebhookSpecification, propertyInfo.GetValue(manifest.WebhookSpecification));
                }
            }

            Username = username;
        }

		public string Username { get; set; } = default!;
    }
}
