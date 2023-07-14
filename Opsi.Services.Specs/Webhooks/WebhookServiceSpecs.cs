using FakeItEasy;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.TableServices;
using Opsi.Services.Webhooks;

namespace Opsi.Services.Specs.Webhooks;

[TestClass]
public class WebhookServiceSpecs
{
    private const string _remoteUriAsString = "https://test.url.com/";
    private const string _webhookEvent = "TEST WEBHOOK EVENT";
    private const string _webhookLevel = "TEST WEBHOOK LEVEL";
    private const string _webhookName = "TEST WEBHOOK NAME";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private WebhookMessage _webhookMessage;
    private ConsumerWebhookSpecification _webhookSpec;
    private InternalWebhookMessage _internalWebhookMessage;
    private IWebhookDispatcher _webhookDispatcher;
    private IWebhookTableService _webhookTableService;
    private WebhookService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _webhookMessage = new WebhookMessage
        {
            Event = _webhookEvent,
            Level = _webhookLevel,
            Name = _webhookName
        };
        _webhookSpec = new ConsumerWebhookSpecification(_remoteUriAsString);
        _internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _webhookSpec);

        _webhookDispatcher = A.Fake<IWebhookDispatcher>();
        _webhookTableService = A.Fake<IWebhookTableService>();

        _testee = new WebhookService(_webhookDispatcher, _webhookTableService);
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsEmpty_DoesNotAttemptDispatch()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = String.Empty;

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>._, A<Uri>._, A<Dictionary<string, object>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsEmpty_StoresNothing()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = String.Empty;

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNotAbsoluteUrl_DoesNotAttemptDispatch()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = "/test/segment";

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>._, A<Uri>._, A<Dictionary<string, object>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNotAbsoluteUrl_StoresNothing()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = "/test/segment";

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNull_DoesNotAttemptDispatch()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = null;

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>._, A<Uri>._, A<Dictionary<string, object>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNull_StoresNothing()
    {
        _internalWebhookMessage.WebhookSpecification.Uri = null;

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageSuccessfullyDispatched_RecordsIsDeliveredAsTrue()
    {
        const bool isDelivered = true;
        var webhookDispatchResponse = new WebhookDispatchResponse { IsSuccessful = isDelivered };

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>.That.Matches(cm => cm.Event.Equals(_webhookMessage.Event)
                                                                                                    && cm.Level.Equals(_webhookLevel)
                                                                                                    && cm.Name.Equals(_webhookName)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString)),
                                                               A<Dictionary<string, object>>._))
            .Returns(webhookDispatchResponse);

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>.That.Matches(icm => icm.IsDelivered == isDelivered))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageSuccessfullyDispatched_RecordsWithoutIncrementedFailureCount()
    {
        const int failureCount = 2;
        const bool isDelivered = true;
        var webhookDispatchResponse = new WebhookDispatchResponse { IsSuccessful = isDelivered };

        _internalWebhookMessage.FailureCount = failureCount;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>.That.Matches(cm => cm.Event.Equals(_webhookMessage.Event)
                                                                                                    && cm.Level.Equals(_webhookLevel)
                                                                                                    && cm.Name.Equals(_webhookName)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString)),
                                                               A<Dictionary<string, object>>._))
            .Returns(webhookDispatchResponse);

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>.That.Matches(icm => icm.FailureCount == failureCount))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageUnsuccessfullyDispatched_RecordsIsDeliveredAsFalse()
    {
        const bool isDelivered = false;
        var webhookDispatchResponse = new WebhookDispatchResponse { IsSuccessful = isDelivered };

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>.That.Matches(cm => cm.Event.Equals(_webhookMessage.Event)
                                                                                                    && cm.Level.Equals(_webhookLevel)
                                                                                                    && cm.Name.Equals(_webhookName)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString)),
                                                               A<Dictionary<string, object>>._))
            .Returns(webhookDispatchResponse);

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>.That.Matches(icm => icm.IsDelivered == isDelivered))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageUnsuccessfullyDispatched_RecordsWithIncrementedFailureCount()
    {
        const int failureCount = 2;
        const int incrementedFailureCount = failureCount + 1;
        const bool isDelivered = false;
        var webhookDispatchResponse = new WebhookDispatchResponse { IsSuccessful = isDelivered };

        _internalWebhookMessage.FailureCount = failureCount;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>.That.Matches(cm => cm.Event.Equals(_webhookMessage.Event)
                                                                                                    && cm.Level.Equals(_webhookLevel)
                                                                                                    && cm.Name.Equals(_webhookName)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString)),
                                                               A<Dictionary<string, object>>._))
            .Returns(webhookDispatchResponse);

        await _testee.AttemptDeliveryAndRecordAsync(_internalWebhookMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalWebhookMessage>.That.Matches(icm => icm.FailureCount == incrementedFailureCount))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task DispatchUndeliveredAsync_GetsUndeliveredWebhooksFromTableService()
    {
        var undeliveredInternalWebhookMessages = GetInternalWebhookMessages().Take(3).ToList();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).Returns(undeliveredInternalWebhookMessages);

        await _testee.DispatchUndeliveredAsync();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task DispatchUndeliveredAsync_WhenUndeliveredWebhooksObtained_AttemptsDeliveryOfEachUndeliveredWebhook()
    {
        var undeliveredInternalWebhookMessages = GetInternalWebhookMessages().Take(3).ToList();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).Returns(undeliveredInternalWebhookMessages);

        await _testee.DispatchUndeliveredAsync();

        foreach (var undeliveredInternalWebhookMessage in undeliveredInternalWebhookMessages)
        {
            A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<WebhookMessage>.That.Matches(cm => cm.Event.Equals(undeliveredInternalWebhookMessage.Event)
                                                                                                        && cm.Level.Equals(undeliveredInternalWebhookMessage.Level)
                                                                                                        && cm.Name.Equals(undeliveredInternalWebhookMessage.Name)),
                                                                   A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString)),
                                                                   A<Dictionary<string, object>>._))
                .MustHaveHappenedOnceExactly();
        }

        // No need to test other functionality inside AttemptDeliveryAndRecordAsync in this test.
    }

    private IEnumerable<InternalWebhookMessage> GetInternalWebhookMessages()
    {
        var statusIndex = 0;

        while (true)
        {
            yield return new InternalWebhookMessage(new WebhookMessage
            {
                Event = statusIndex++.ToString(),
                Level = statusIndex.ToString(),
                Name = statusIndex.ToString()
            }, _webhookSpec)
            {
                FailureCount = 2,
                IsDelivered = false,
            };
        }
    }
}
