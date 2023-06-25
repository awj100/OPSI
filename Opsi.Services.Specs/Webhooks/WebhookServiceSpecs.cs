using FakeItEasy;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.TableServices;
using Opsi.Services.Webhooks;

namespace Opsi.Services.Specs.Webhooks;

[TestClass]
public class WebhookServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private CallbackMessage _callbackMessage;
    private InternalCallbackMessage _internalCallbackMessage;
    private string _remoteUriAsString = "https://test.url.com/";
    private IWebhookDispatcher _webhookDispatcher;
    private IWebhookTableService _webhookTableService;
    private WebhookService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _callbackMessage = new CallbackMessage { Status = Guid.NewGuid().ToString() };
        _internalCallbackMessage = new InternalCallbackMessage(_callbackMessage, _remoteUriAsString);

        _webhookDispatcher = A.Fake<IWebhookDispatcher>();
        _webhookTableService = A.Fake<IWebhookTableService>();

        _testee = new WebhookService(_webhookDispatcher, _webhookTableService);
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsEmpty_DoesNotAttemptDispatch()
    {
        _internalCallbackMessage.RemoteUri = String.Empty;

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>._, A<Uri>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsEmpty_StoresNothing()
    {
        _internalCallbackMessage.RemoteUri = String.Empty;

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNotAbsoluteUrl_DoesNotAttemptDispatch()
    {
        _internalCallbackMessage.RemoteUri = "/test/segment";

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>._, A<Uri>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNotAbsoluteUrl_StoresNothing()
    {
        _internalCallbackMessage.RemoteUri = "/test/segment";

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNull_DoesNotAttemptDispatch()
    {
        _internalCallbackMessage.RemoteUri = null;

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>._, A<Uri>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenRemoteUriIsNull_StoresNothing()
    {
        _internalCallbackMessage.RemoteUri = null;

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageSuccessfullyDispatched_RecordsIsDeliveredAsTrue()
    {
        const bool isDelivered = true;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals(_callbackMessage.Status)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString))))
            .Returns(isDelivered);

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>.That.Matches(icm => icm.IsDelivered == isDelivered))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageSuccessfullyDispatched_RecordsWithoutIncrementedFailureCount()
    {
        const int failureCount = 2;
        const bool isDelivered = true;

        _internalCallbackMessage.FailureCount = failureCount;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals(_callbackMessage.Status)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString))))
            .Returns(isDelivered);

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>.That.Matches(icm => icm.FailureCount == failureCount))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageUnsuccessfullyDispatched_RecordsIsDeliveredAsFalse()
    {
        const bool isDelivered = false;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals(_callbackMessage.Status)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString))))
            .Returns(isDelivered);

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>.That.Matches(icm => icm.IsDelivered == isDelivered))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AttemptDeliveryAndRecordAsync_WhenMessageUnsuccessfullyDispatched_RecordsWithIncrementedFailureCount()
    {
        const int failureCount = 2;
        const int incrementedFailureCount = failureCount + 1;
        const bool isDelivered = false;

        _internalCallbackMessage.FailureCount = failureCount;

        A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals(_callbackMessage.Status)),
                                                               A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString))))
            .Returns(isDelivered);

        await _testee.AttemptDeliveryAndRecordAsync(_internalCallbackMessage);

        A.CallTo(() => _webhookTableService.StoreAsync(A<InternalCallbackMessage>.That.Matches(icm => icm.FailureCount == incrementedFailureCount))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task DispatchUndeliveredAsync_GetsUndeliveredCallbacksFromTableService()
    {
        var undeliveredInternalCallbackMessages = GetInternalCallbackMessages().Take(3).ToList();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).Returns(undeliveredInternalCallbackMessages);

        await _testee.DispatchUndeliveredAsync();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task DispatchUndeliveredAsync_WhenUndeliveredCallbacksObtained_AttemptsDeliveryOfEachUndeliveredCallback()
    {
        var undeliveredInternalCallbackMessages = GetInternalCallbackMessages().Take(3).ToList();

        A.CallTo(() => _webhookTableService.GetUndeliveredAsync()).Returns(undeliveredInternalCallbackMessages);

        await _testee.DispatchUndeliveredAsync();

        foreach (var undeliveredInternalCallbackMessage in undeliveredInternalCallbackMessages)
        {
            A.CallTo(() => _webhookDispatcher.AttemptDeliveryAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals(undeliveredInternalCallbackMessage.Status)),
                                                                   A<Uri>.That.Matches(uri => uri.AbsoluteUri.Equals(_remoteUriAsString))))
                .MustHaveHappenedOnceExactly();
        }

        // No need to test other functionality inside AttemptDeliveryAndRecordAsync in this test.
    }

    private IEnumerable<InternalCallbackMessage> GetInternalCallbackMessages()
    {
        var statusIndex = 0;

        while (true)
        {
            yield return new InternalCallbackMessage
            {
                FailureCount = 2,
                IsDelivered = false,
                RemoteUri = _remoteUriAsString,
                Status = statusIndex++.ToString()
            };
        }
    }
}
