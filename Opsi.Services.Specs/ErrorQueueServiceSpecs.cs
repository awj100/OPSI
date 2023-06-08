using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ErrorQueueServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ILogger<ErrorQueueService> _logger;
    private ILoggerFactory _loggerFactory;
    private IQueueService _queueService;
    private IQueueServiceFactory _queueServiceFactory;
    private ErrorQueueService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _loggerFactory = new NullLoggerFactory();
        _queueService = A.Fake<IQueueService>();
        _queueServiceFactory = A.Fake<IQueueServiceFactory>();

        A.CallTo(() => _queueServiceFactory.Create(Constants.QueueNames.Error)).Returns(_queueService);

        _testee = new ErrorQueueService(_loggerFactory, _queueServiceFactory);
    }

    [TestMethod]
    public async Task ReportAsync_QueuesErrorWhichWrapsSpecifiedException()
    {
        var exception = new Exception(Guid.NewGuid().ToString());
        var exceptionOrigin = nameof(ReportAsync_QueuesErrorWhichWrapsSpecifiedException);

        await _testee.ReportAsync(exception);

        A.CallTo(() => _queueService.AddMessageAsync(A<Error>.That.Matches(e => e.Origin.Equals(exceptionOrigin)
                                                                                && e.Message.Equals(exception.Message))))
            .MustHaveHappenedOnceExactly();
    }
}
