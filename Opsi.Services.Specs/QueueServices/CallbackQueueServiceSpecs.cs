using FakeItEasy;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs.QueueServices;

[TestClass]
public class CallbackQueueServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private InternalCallbackMessage _internalCallbackMessage;
    private IErrorQueueService _errorQueueService;
    private IQueueService _queueService;
    private IQueueServiceFactory _queueServiceFactory;
    private CallbackQueueService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _internalCallbackMessage = new InternalCallbackMessage
        {
            ProjectId = Guid.NewGuid(),
            RemoteUri = "https://a.test.url",
            Status = Guid.NewGuid().ToString()
        };

        _errorQueueService = A.Fake<IErrorQueueService>();
        _queueService = A.Fake<IQueueService>();
        _queueServiceFactory = A.Fake<IQueueServiceFactory>();

        A.CallTo(() => _queueServiceFactory.Create(Constants.QueueNames.Callback)).Returns(_queueService);

        _testee = new CallbackQueueService(_queueServiceFactory, _errorQueueService);
    }

    [TestMethod]
    public async Task QueueCallbackAsync_QueuesSpecifiedCallbackMessage()
    {
        await _testee.QueueCallbackAsync(_internalCallbackMessage);

        A.CallTo(() => _queueService.AddMessageAsync(_internalCallbackMessage)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task QueueCallbackAsync_WhenQueuingCallbackFails_QueuesException()
    {
        var exception = new Exception(Guid.NewGuid().ToString());
        A.CallTo(() => _queueService.AddMessageAsync(A<CallbackMessage>._)).Throws(exception);

        await _testee.QueueCallbackAsync(_internalCallbackMessage);

        A.CallTo(() => _errorQueueService.ReportAsync(exception,
                                                      A<LogLevel>._,
                                                      A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task QueueCallbackAsync_WhenRemoteUriIsEmpty_DoesNotQueueCallbackMessage()
    {
        var remoteUri = String.Empty;

        _internalCallbackMessage.RemoteUri = remoteUri;

        await _testee.QueueCallbackAsync(_internalCallbackMessage);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();

        // ---

        await _testee.QueueCallbackAsync(_internalCallbackMessage, remoteUri);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task QueueCallbackAsync_WhenRemoteUriIsNull_DoesNotQueueCallbackMessage()
    {
        const string? remoteUri = null;

        _internalCallbackMessage.RemoteUri = remoteUri;

        await _testee.QueueCallbackAsync(_internalCallbackMessage);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();

        // ---

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await _testee.QueueCallbackAsync(_internalCallbackMessage, remoteUri);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalCallbackMessage>._)).MustNotHaveHappened();
    }
}
