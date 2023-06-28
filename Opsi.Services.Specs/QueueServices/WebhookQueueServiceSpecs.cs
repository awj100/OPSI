using FakeItEasy;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs.QueueServices;

[TestClass]
public class WebhookQueueServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private InternalWebhookMessage _internalWebhookMessage;
    private IErrorQueueService _errorQueueService;
    private IQueueService _queueService;
    private IQueueServiceFactory _queueServiceFactory;
    private WebhookQueueService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _internalWebhookMessage = new InternalWebhookMessage
        {
            ProjectId = Guid.NewGuid(),
            RemoteUri = "https://a.test.url",
            Status = Guid.NewGuid().ToString()
        };

        _errorQueueService = A.Fake<IErrorQueueService>();
        _queueService = A.Fake<IQueueService>();
        _queueServiceFactory = A.Fake<IQueueServiceFactory>();

        A.CallTo(() => _queueServiceFactory.Create(Constants.QueueNames.Webhook)).Returns(_queueService);

        _testee = new WebhookQueueService(_queueServiceFactory, _errorQueueService);
    }

    [TestMethod]
    public async Task QueueWebhookMessageAsync_QueuesSpecifiedWebhookMessage()
    {
        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage);

        A.CallTo(() => _queueService.AddMessageAsync(_internalWebhookMessage)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task QueueWebhookMessageAsync_WhenQueuingWebhookFails_QueuesException()
    {
        var exception = new Exception(Guid.NewGuid().ToString());
        A.CallTo(() => _queueService.AddMessageAsync(A<WebhookMessage>._)).Throws(exception);

        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage);

        A.CallTo(() => _errorQueueService.ReportAsync(exception,
                                                      A<LogLevel>._,
                                                      A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task QueueWebhookMessageAsync_WhenRemoteUriIsEmpty_DoesNotQueueWebhookMessage()
    {
        var remoteUri = String.Empty;

        _internalWebhookMessage.RemoteUri = remoteUri;

        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();

        // ---

        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage, remoteUri);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task QueueWebhookMessageAsync_WhenRemoteUriIsNull_DoesNotQueueWebhookMessage()
    {
        const string? remoteUri = null;

        _internalWebhookMessage.RemoteUri = remoteUri;

        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();

        // ---

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await _testee.QueueWebhookMessageAsync(_internalWebhookMessage, remoteUri);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
    }
}
