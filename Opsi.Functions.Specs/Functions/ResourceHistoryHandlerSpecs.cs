using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Functions.Functions;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Specs.Functions;

[TestClass]
public class ResourceHistoryHandlerSpecs
{
    private Guid _projectId = Guid.NewGuid();
    private const string _restOfPath = "folder/filename.ext";
    private const string _username = "user@test.com";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IErrorQueueService _errorQueueService;
    private ILoggerFactory _loggerFactory;
    private IResourceService _resourceService;
    private IResponseSerialiser _responseSerialiser;
    private ResourceHistoryHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _errorQueueService = A.Fake<IErrorQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _resourceService = A.Fake<IResourceService>();
        _responseSerialiser = A.Fake<IResponseSerialiser>();

        _testee = new ResourceHistoryHandler(_resourceService,
                                             _errorQueueService,
                                             _responseSerialiser,
                                             _loggerFactory);
    }

    [TestMethod]
    public async Task Run_When()
    {
    }
}
