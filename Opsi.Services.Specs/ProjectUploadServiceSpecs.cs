using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Functions.FormHelpers;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectUploadServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private const string HandlerQueueName = "test handler queue";
    private IFormFileCollection _formFileCollection;
    private Manifest _manifest;
    private Stream _manifestStream;
    private Stream _nonManifestStream;

    private IBlobService _blobService;
    private ICallbackQueueService _callbackQueueService;
    private ILoggerFactory _loggerFactory;
    private IManifestService _manifestService;
    private IQueueService _queueService;
    private IQueueServiceFactory _queueServiceFactory;
    private ProjectUploadService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _manifest = new Manifest { HandlerQueue = HandlerQueueName };
        _manifestStream = new MemoryStream();
        _nonManifestStream = new MemoryStream();
        _formFileCollection = new FormFileCollection
        {
            { ManifestService.ManifestName, _manifestStream },
            { "non_manifest_object", _nonManifestStream }
        };

        _blobService = A.Fake<IBlobService>();
        _callbackQueueService = A.Fake<ICallbackQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _manifestService = A.Fake<IManifestService>();
        _queueServiceFactory = A.Fake<IQueueServiceFactory>();
        _queueService = A.Fake<IQueueService>();

        A.CallTo(() => _manifestService.GetManifestAsync(_formFileCollection)).Returns(_manifest);
        A.CallTo(() => _queueServiceFactory.Create(A<string>.That.Contains(HandlerQueueName))).Returns(_queueService);

        _testee = new ProjectUploadService(_manifestService,
                                           _callbackQueueService,
                                           _queueServiceFactory,
                                           _blobService,
                                           _loggerFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _manifestStream?.Dispose();
        _nonManifestStream?.Dispose();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_WhenUploadedObjectCountGreaterThanRequiredCount_ThrowsBadRequestException()
    {
        var formFilesCollection = A.Fake<IFormFileCollection>();
        A.CallTo(() => formFilesCollection.Count).Returns(_testee.RequiredNumberOfUploadedObjects + 1);

        await _testee.Invoking(t => t.StoreInitialProjectUploadAsync(formFilesCollection))
                     .Should()
                     .ThrowAsync<BadRequestException>();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_WhenUploadedObjectCountLessThanRequiredCount_ThrowsBadRequestException()
    {
        var formFilesCollection = A.Fake<IFormFileCollection>();
        A.CallTo(() => formFilesCollection.Count).Returns(_testee.RequiredNumberOfUploadedObjects - 1);

        await _testee.Invoking(t => t.StoreInitialProjectUploadAsync(formFilesCollection))
                     .Should()
                     .ThrowAsync<BadRequestException>();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_ObtainsManifestFromFormFileCollection()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _manifestService.GetManifestAsync(_formFileCollection)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_UsesManifestQueueHandlerToObtainQueueService()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueServiceFactory.Create(A<string>.That.Contains(HandlerQueueName))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesManifestOnCorrectQueue()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueService.AddMessageAsync(_manifest)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesCallbackWithCorrectProjectId()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_manifest.ProjectId)))).MustHaveHappenedOnceExactly();
    }
}
