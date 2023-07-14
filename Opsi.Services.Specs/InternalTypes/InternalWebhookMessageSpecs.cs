using System.Reflection;
using FluentAssertions;
using Opsi.Pocos;

namespace Opsi.Services.Specs.InternalTypes;

[TestClass]
public class InternalWebhookMessageSpecs
{
    private const string _customProp1Name = nameof(_customProp1Name);
    private const string _customProp1Value = nameof(_customProp1Value);
    private const string _customProp2Name = nameof(_customProp2Name);
    private const int _customProp2Value = 2;
    private const string _event = "TEST EVENT";
    private const string _level = "TEST LEVEL";
    private const string _name = "TEST NAME";
    private const string _status = "TEST STATUS";
    private const string _uri = "https://a.test.url";
    private const string _username = "TEST USERNAME";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Dictionary<string, object> _customProps;
    private Guid _id;
    private InternalWebhookMessage _internalWebhookMessage;
    private DateTime _occurredOn;
    private Guid _projectId;
    private WebhookMessage _webhookMessage;
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

        _webhookSpec = new ConsumerWebhookSpecification
        {
            CustomProps = _customProps,
            Uri = _uri
        };

        _webhookMessage = new WebhookMessage
        {
            Event = _event,
            Id = _id,
            Level = _level,
            Name = _name,
            OccurredOn = _occurredOn,
            ProjectId = _projectId,
            Username = _username
        };

        _internalWebhookMessage = new InternalWebhookMessage
        {
            Event = _event,
            Id = _id,
            Level = _level,
            Name = _name,
            OccurredOn = _occurredOn,
            ProjectId = _projectId,
            Username = _username,
            WebhookSpecification = _webhookSpec
        };
    }

    [TestMethod]
    public void Ctor_WhenPassedWebhookMessage_SetsPropertiesWithExpectedValues()
    {
        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _webhookSpec);

        foreach(var propInfo in _webhookMessage.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public))
        {
            var webhookValue = propInfo.GetValue(_webhookMessage);
            var internalWebhookValue = propInfo.GetValue(internalWebhookMessage);

            internalWebhookValue.Should().Be(webhookValue);
        }
    }

    [TestMethod]
    public void Ctor_WhenPassedConsumerWebhookSpecification_SetsPropertiesWithExpectedValues()
    {
        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _webhookSpec);

        foreach (var propInfo in _webhookSpec.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var webhookValue = propInfo.GetValue(_webhookSpec);
            var internalWebhookValue = propInfo.GetValue(internalWebhookMessage.WebhookSpecification);

            internalWebhookValue.Should().Be(webhookValue);
        }
    }

    [TestMethod]
    public void ToWebhookMessage_ReturnsWebhookMessageWithExpectedValues()
    {
        var webhookMessage = _internalWebhookMessage.ToWebhookMessage();

        foreach (var propInfo in webhookMessage.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var webhookValue = propInfo.GetValue(_webhookMessage);
            var internalWebhookValue = propInfo.GetValue(_internalWebhookMessage);

            webhookValue.Should().Be(internalWebhookValue);
        }
    }
}
