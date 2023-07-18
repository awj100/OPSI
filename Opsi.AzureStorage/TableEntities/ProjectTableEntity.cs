using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Opsi.Pocos;

namespace Opsi.AzureStorage.TableEntities;

public class ProjectTableEntity : ProjectBase, ITableEntity
{
    public ProjectTableEntity()
    {
        PartitionKey = GetDefaultPartitionKey();
        RowKey = GetDefaultRowKey();
    }

    public static ProjectTableEntity FromProject(Project project)
    {
        var projectTableEntity = new ProjectTableEntity
        {
            Id = project.Id,
            Name = project.Name,
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

    public ETag ETag { get; set; } = default!;

    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; } = default!;

    public string? WebhookCustomProps { get; set; }

    public string? WebhookUri { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }

    /// <summary>
    /// The partition key should be a descending, non-unique number so that retrieving multiple projects
    /// will return the newest project first.
    /// </summary>
    private static string GetDefaultPartitionKey()
    {
        return (int.MaxValue - Convert.ToInt32(DateTime.UtcNow.ToString("yyyyMMdd"))).ToString();
    }

    /// <summary>
    /// The row key should be a descending, unique number so that retrieving multiple projects
    /// will return the newest project first.
    /// </summary>
    private static string GetDefaultRowKey()
    {
        return $"{long.MaxValue - DateTime.UtcNow.Ticks}-{Guid.NewGuid()}";
    }
}
