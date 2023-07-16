using System.Text.Json;
using FluentAssertions;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;

namespace Opsi.AzureStorage.Specs;

[TestClass]
public class InternalWebhookMessageTableEntitySpecs
{
    private const string _customProp1Name = nameof(_customProp1Name);
    private const string _customProp1Value = nameof(_customProp1Value);
    private const string _customProp2Name = nameof(_customProp2Name);
    private const int _customProp2Value = 2;
    private const string _event = "TEST EVENT";
    private const int _failureCount = 9;
    private const bool _isDelivered = true;
    private const string _lastFailureReason = "TEST LAST FAILURE REASON";
    private const string _level = "TEST LEVEL";
    private const string _name = "TEST NAME";
    private const string _status = "TEST STATUS";
    private const string _uri = "https://a.test.url";
    private const string _username = "TEST USERNAME";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Dictionary<string, object> _customProps;
    private Guid _id;
    private DateTime _occurredOn;
    private Guid _projectId;
    private InternalWebhookMessage _internalWebhookMessage;
    private InternalWebhookMessageTableEntity _internalWebhookMessageTableEntity;
    private string _serialisedCustomProps;
    private ConsumerWebhookSpecification _webhookSpec;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _id = Guid.NewGuid();
        _occurredOn = DateTime.Now;
        _projectId = Guid.NewGuid();

        _customProps = new Dictionary<string, object>
        {
            {_customProp1Name, _customProp1Value},
            {_customProp2Name, _customProp2Value }
        };

        _serialisedCustomProps = JsonSerializer.Serialize(_customProps);

        _webhookSpec = new ConsumerWebhookSpecification
        {
            CustomProps = _customProps,
            Uri = _uri
        };

        _internalWebhookMessage = new InternalWebhookMessage
        {
            Event = _event,
            FailureCount = _failureCount,
            Id = _id,
            IsDelivered = _isDelivered,
            LastFailureReason = _lastFailureReason,
            Level = _level,
            Name = _name,
            OccurredOn = _occurredOn,
            ProjectId = _projectId,
            Username = _username,
            WebhookSpecification = _webhookSpec
        };

        _internalWebhookMessageTableEntity = new InternalWebhookMessageTableEntity
        {
            Event = _event,
            FailureCount = _failureCount,
            Id = _id,
            IsDelivered = _isDelivered,
            LastFailureReason = _lastFailureReason,
            Level = _level,
            Name = _name,
            OccurredOn = _occurredOn,
            ProjectId = _projectId,
            SerialisedWebhookCustomProps = _serialisedCustomProps,
            Username = _username
        };
    }

    [TestMethod]
    public void FromInternalWebhookMessage_ReturnsObjectWithExpectedPropertyValues()
    {
        var tableEntity = InternalWebhookMessageTableEntity.FromInternalWebhookMessage(_internalWebhookMessage);

        tableEntity.Event.Should().Be(_internalWebhookMessage.Event);
        tableEntity.FailureCount.Should().Be(_internalWebhookMessage.FailureCount);
        tableEntity.Id.Should().Be(_internalWebhookMessage.Id);
        tableEntity.IsDelivered.Should().Be(_internalWebhookMessage.IsDelivered);
        tableEntity.LastFailureReason.Should().Be(_internalWebhookMessage.LastFailureReason);
        tableEntity.Level.Should().Be(_internalWebhookMessage.Level);
        tableEntity.Name.Should().Be(_internalWebhookMessage.Name);
        tableEntity.OccurredOn.Should().Be(_internalWebhookMessage.OccurredOn);
        tableEntity.ProjectId.Should().Be(_internalWebhookMessage.ProjectId);
        tableEntity.SerialisedWebhookCustomProps.Should().NotBeNull();
        tableEntity.Username.Should().Be(_internalWebhookMessage.Username);
        tableEntity.SerialisedWebhookCustomProps.Should().NotBeNull();
        tableEntity.WebhookUri.Should().Be(_internalWebhookMessage.WebhookSpecification!.Uri);

        var deserialisedCustomProps = JsonSerializer.Deserialize<Dictionary<string, object>>(tableEntity.SerialisedWebhookCustomProps);
        deserialisedCustomProps.Should().HaveCount(_internalWebhookMessage.WebhookSpecification.CustomProps!.Count);
    }

    [TestMethod]
    public void FromInternalWebhookMessage_SetsPartitionKeyFromProjectId()
    {
        var tableEntity = InternalWebhookMessageTableEntity.FromInternalWebhookMessage(_internalWebhookMessage);

        tableEntity.PartitionKey.Should().Be(_internalWebhookMessage.ProjectId.ToString());
    }

    [TestMethod]
    public void FromInternalWebhookMessage_SetsRowKeyFromId()
    {
        var tableEntity = InternalWebhookMessageTableEntity.FromInternalWebhookMessage(_internalWebhookMessage);

        tableEntity.RowKey.Should().Be(_internalWebhookMessage.Id.ToString());
    }

    [TestMethod]
    public void ToInternalWebhookMessage_ReturnsObjectWithExpectedPropertyValues()
    {
        var internalWebhookMessage = _internalWebhookMessageTableEntity.ToInternalWebhookMessage();

        internalWebhookMessage.Event.Should().Be(_internalWebhookMessageTableEntity.Event);
        internalWebhookMessage.FailureCount.Should().Be(_internalWebhookMessageTableEntity.FailureCount);
        internalWebhookMessage.Id.Should().Be(_internalWebhookMessageTableEntity.Id);
        internalWebhookMessage.IsDelivered.Should().Be(_internalWebhookMessageTableEntity.IsDelivered);
        internalWebhookMessage.LastFailureReason.Should().Be(_internalWebhookMessageTableEntity.LastFailureReason);
        internalWebhookMessage.Level.Should().Be(_internalWebhookMessageTableEntity.Level);
        internalWebhookMessage.Name.Should().Be(_internalWebhookMessageTableEntity.Name);
        internalWebhookMessage.OccurredOn.Should().Be(_internalWebhookMessageTableEntity.OccurredOn);
        internalWebhookMessage.ProjectId.Should().Be(_internalWebhookMessageTableEntity.ProjectId);
        internalWebhookMessage.Username.Should().Be(_internalWebhookMessageTableEntity.Username);
        internalWebhookMessage.WebhookSpecification.Should().NotBeNull();
        internalWebhookMessage.WebhookSpecification!.Uri.Should().Be(_internalWebhookMessageTableEntity.WebhookUri);
        internalWebhookMessage.WebhookSpecification.CustomProps.Should().HaveCount(_customProps.Count);
    }
}
