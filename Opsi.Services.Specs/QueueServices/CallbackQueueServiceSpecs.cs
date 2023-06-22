using FakeItEasy;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs.QueueServices;

[TestClass]
public class CallbackQueueServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private CallbackMessage _callbackMessage;
    private IErrorQueueService _errorQueueService;
    private IQueueService _queueService;
    private IQueueServiceFactory _queueServiceFactory;
    private CallbackQueueService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _callbackMessage = new CallbackMessage
        {
            ProjectId = Guid.NewGuid(),
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
        await _testee.QueueCallbackAsync(_callbackMessage);

        A.CallTo(() => _queueService.AddMessageAsync(_callbackMessage)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task QueueCallbackAsync_WhenQueuingCallbackFails_QueuesException()
    {
        var exception = new Exception(Guid.NewGuid().ToString());
        A.CallTo(() => _queueService.AddMessageAsync(A<CallbackMessage>._)).Throws(exception);

        await _testee.QueueCallbackAsync(_callbackMessage);

        A.CallTo(() => _errorQueueService.ReportAsync(exception,
                                                      A<LogLevel>._,
                                                      A<string>._)).MustHaveHappenedOnceExactly();
    }
}
