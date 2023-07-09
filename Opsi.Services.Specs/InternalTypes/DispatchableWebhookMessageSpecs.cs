using System.Reflection;
using FluentAssertions;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.Specs.InternalTypes;

[TestClass]
public class DispatchableWebhookMessageSpecs
{
    private const string _customProp1Name = nameof(_customProp1Name);
    private const string _customProp1Value = nameof(_customProp1Value);
    private const string _customProp2Name = nameof(_customProp2Name);
    private const int _customProp2Value = 2;
    private const string _status = "TEST STATUS";
    private const string _username = "user@test.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Dictionary<string, object> _customProps;
    private Guid _id;
    private DateTime _occurredOn;
    private Guid _projectId;
    private WebhookMessage _webhookMessage;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _id = Guid.NewGuid();
        _occurredOn = DateTime.Now;
        _projectId = Guid.NewGuid();

        _customProps = new Dictionary<string, object>
        {
            {_customProp1Name, _customProp1Value },
            {_customProp2Name, _customProp2Value }
        };

        _webhookMessage = new WebhookMessage
        {
            Id = _id,
            OccurredOn = _occurredOn,
            ProjectId = _projectId,
            Status = _status,
            Username = _username
        };
    }

    [TestMethod]
    public void FromWebhookMessage_ReturnsPropertiesWithSpecifiedWebhookMessageValues()
    {
        var dispatchableWebhookMessage = DispatchableWebhookMessage.FromWebhookMessage(_webhookMessage, _customProps);

        dispatchableWebhookMessage.Should().NotBeNull();

        foreach(var propInfo in _webhookMessage.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public))
        {
            var webhookMessageValue = propInfo.GetValue(_webhookMessage);
            var dispatchableWebhookMessageValue = propInfo.GetValue(dispatchableWebhookMessage);

            dispatchableWebhookMessageValue.Should().Be(webhookMessageValue);
        }
    }

    [TestMethod]
    public void FromWebhookMessage_ReturnsPropertiesWithSpecifiedCustomPropsValues()
    {
        var dispatchableWebhookMessage = DispatchableWebhookMessage.FromWebhookMessage(_webhookMessage, _customProps);

        dispatchableWebhookMessage.Should().NotBeNull();
        dispatchableWebhookMessage.CustomProps.Should().NotBeNullOrEmpty();
        dispatchableWebhookMessage.CustomProps.Should().HaveCount(_customProps.Count);
    }
}
