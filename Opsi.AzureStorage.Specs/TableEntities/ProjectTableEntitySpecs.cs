using System.Text.Json;
using FluentAssertions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;

namespace Opsi.AzureStorage.Specs;

[TestClass]
public class ProjectTableEntitySpecs
{
    private const string _name = "TEST PROJECT NAME";
    private const string _state = "TEST STATE";
    private const string _uri = "https://a.test.url";
    private const string _username = "user@test.com";
    private Guid _id;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Dictionary<string, object> _customProps;
    private Project _project;
    private ProjectTableEntity _projectTableEntity;
    private string _serialisedCustomProps;
    private ConsumerWebhookSpecification _webhoookSpec;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _id = Guid.NewGuid();

        _customProps = new Dictionary<string, object>
        {
            { "customProp1Name", "customProp1Value" },
            { "customProp2Name", 2 }
        };

        _project = new Project
        {
            Id = _id,
            Name = _name,
            State = _state,
            Username = _username
        };

        _projectTableEntity = new ProjectTableEntity
        {
            Id = _id,
            Name = _name,
            State = _state,
            Username = _username
        };

        _serialisedCustomProps = JsonSerializer.Serialize(_customProps);

        _webhoookSpec = new ConsumerWebhookSpecification
        {
            CustomProps = _customProps,
            Uri = _uri
        };
    }

    [TestMethod]
    public void FromProject_WhenWebhookSpecificationsIsNull_ReturnsObjectWithExpectedPropertyValues()
    {
        var projectTableEntity = ProjectTableEntity.FromProject(_project);

        projectTableEntity.Id.Should().Be(_project.Id);
        projectTableEntity.Name.Should().Be(_project.Name);
        projectTableEntity.State.Should().Be(_project.State);
        projectTableEntity.Username.Should().Be(_project.Username);

        projectTableEntity.WebhookCustomProps.Should().BeNull();
        projectTableEntity.WebhookUri.Should().BeNull();
    }

    [TestMethod]
    public void FromProject_WhenWebhookSpecificationsIsNotNull_ReturnsObjectWithExpectedPropertyValues()
    {
        _project.WebhookSpecification = _webhoookSpec;
        var projectTableEntity = ProjectTableEntity.FromProject(_project);

        projectTableEntity.Id.Should().Be(_project.Id);
        projectTableEntity.Name.Should().Be(_project.Name);
        projectTableEntity.State.Should().Be(_project.State);
        projectTableEntity.Username.Should().Be(_project.Username);

        projectTableEntity.WebhookCustomProps.Should().Be(_serialisedCustomProps);
        projectTableEntity.WebhookUri.Should().Be(_webhoookSpec.Uri);
    }

    [TestMethod]
    public void ToProject_WhenWebhookSpecificationsIsNull_ReturnsObjectWithExpectedPropertyValues()
    {
        var project = _projectTableEntity.ToProject();

        project.Id.Should().Be(project.Id);
        project.Name.Should().Be(project.Name);
        project.State.Should().Be(project.State);
        project.Username.Should().Be(project.Username);
        project.WebhookSpecification.Should().BeNull();
    }

    [TestMethod]
    public void ToProject_WhenWebhookSpecificationsIsNotNull_ReturnsObjectWithExpectedPropertyValues()
    {
        _projectTableEntity.WebhookCustomProps = _serialisedCustomProps;
        _projectTableEntity.WebhookUri = _webhoookSpec.Uri;
        var project = _projectTableEntity.ToProject();

        project.Id.Should().Be(project.Id);
        project.Name.Should().Be(project.Name);
        project.State.Should().Be(project.State);
        project.Username.Should().Be(project.Username);
        project.WebhookSpecification.Should().NotBeNull();
        project.WebhookSpecification.Uri.Should().Be(_uri);
        project.WebhookSpecification.CustomProps.Should().HaveCount(_customProps.Count);
    }
}
