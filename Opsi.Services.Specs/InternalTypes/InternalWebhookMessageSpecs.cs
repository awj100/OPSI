using FluentAssertions;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.Specs.InternalTypes;

[TestClass]
public class InternalWebhookMessageSpecs
{
    [TestMethod]
    public void Ctor_WhenPassedWebhookMessage_SetsPartitionKeyFromProjectId()
    {
        const string remoteUri = "https://test.url.com";
        var webhookMessage = new WebhookMessage { ProjectId = Guid.NewGuid() };

        var internalWebhookMessage = new InternalWebhookMessage(webhookMessage, remoteUri);

        internalWebhookMessage.PartitionKey.Should().Be(webhookMessage.ProjectId.ToString());
    }

    [TestMethod]
    public void Ctor_WhenPassedWebhookMessage_SetsRowKeyFromId()
    {
        const string remoteUri = "https://test.url.com";
        var webhookMessage = new WebhookMessage();

        var internalWebhookMessage = new InternalWebhookMessage(webhookMessage, remoteUri);

        internalWebhookMessage.RowKey.Should().Be(webhookMessage.Id.ToString());
    }
}
