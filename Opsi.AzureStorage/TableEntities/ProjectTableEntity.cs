using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class ProjectTableEntity : ProjectBase, ITableEntity
{
    public string EntityType { get; set; } = typeof(ProjectTableEntity).Name;

    public int EntityVersion { get; set; } = 1;

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public string? WebhookCustomProps { get; set; }

    public string? WebhookUri { get; set; }

    public Project ToProject()
    {
        var project = new Project
        {
            Id = Id,
            Name = Name,
            State = State,
            Username = Username
        };

        if (!String.IsNullOrWhiteSpace(this.WebhookUri))
        {
            var webhookSpec = new ConsumerWebhookSpecification { Uri = this.WebhookUri };

            if (!String.IsNullOrWhiteSpace(this.WebhookCustomProps))
            {
                try
                {
                    webhookSpec.CustomProps = JsonSerializer.Deserialize<Dictionary<string, object>>(WebhookCustomProps) ?? new Dictionary<string, object>(0);
                }
                catch (Exception exception)
                {
                    webhookSpec.CustomProps = new Dictionary<string, object>
                    {
                        {exception.GetType().Name, exception.Message }
                    };
                }
            }

            project.WebhookSpecification = webhookSpec;
        }

        return project;
    }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }

    public static ProjectTableEntity FromProject(Project project, string partitionKey, string rowKey)
    {
        var projectTableEntity = new ProjectTableEntity
        {
            Id = project.Id,
            Name = project.Name,
            PartitionKey = partitionKey,
            RowKey = rowKey,
            State = project.State,
            Username = project.Username,
            WebhookUri = project.WebhookSpecification?.Uri
        };

        if (project.WebhookSpecification?.CustomProps?.Count > 0)
        {
            try
            {
                projectTableEntity.WebhookCustomProps = JsonSerializer.Serialize(project.WebhookSpecification.CustomProps);
            }
            catch (Exception exception)
            {
                projectTableEntity.WebhookCustomProps = $"Unable to serialise {nameof(Manifest)}.{nameof(Manifest.WebhookSpecification)}.{nameof(Manifest.WebhookSpecification.CustomProps)}: {exception.Message}";
            }
        }

        return projectTableEntity;
    }

    public static IReadOnlyCollection<ProjectTableEntity> FromProject(Project project, Func<Project, IReadOnlyCollection<KeyPolicy>> keyPolicyResolvers)
    {
        return keyPolicyResolvers(project).Select(keyPolicy => FromProject(project,
                                                                           keyPolicy.PartitionKey,
                                                                           keyPolicy.RowKey.Value)).ToList();
    }
}
