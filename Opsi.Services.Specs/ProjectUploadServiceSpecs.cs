using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Functions.FormHelpers;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectUploadServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private const string AuthHeaderScheme = "TestScheme";
    private const string AuthHeaderValue = "Test auth header value";
    private const string CallbackUri = "Test callback URI";
    private const string HandlerQueueName = "Test handler queue";
    private const string PackageName = "Test package name";
    private const string Username = "user@test.com";

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
    private IUserProvider _userProvider;
    private ProjectUploadService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _manifest = new Manifest
        {
            CallbackUri = CallbackUri,
            HandlerQueue = HandlerQueueName,
            PackageName = PackageName,
            ProjectId = Guid.NewGuid()
        };
        var serialisedManifest = JsonSerializer.Serialize(_manifest);
        _manifestStream = new MemoryStream(Encoding.UTF8.GetBytes(serialisedManifest));
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
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _manifestService.GetManifestAsync(_formFileCollection)).Returns(_manifest);
        A.CallTo(() => _queueServiceFactory.Create(A<string>.That.Contains(HandlerQueueName, StringComparison.OrdinalIgnoreCase))).Returns(_queueService);
        A.CallTo(() => _userProvider.AuthHeader).Returns(new Lazy<AuthenticationHeaderValue>(() => new AuthenticationHeaderValue(AuthHeaderScheme, AuthHeaderValue)));
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => Username));

        _testee = new ProjectUploadService(_manifestService,
                                           _callbackQueueService,
                                           _queueServiceFactory,
                                           _blobService,
                                           _userProvider,
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

        A.CallTo(() => _queueServiceFactory.Create(A<string>.That.Contains(HandlerQueueName, StringComparison.OrdinalIgnoreCase))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesInternalManifestWithCorrectCallbackUri()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalManifest>.That.Matches(m => m.CallbackUri.Equals(_manifest.CallbackUri))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesInternalManifestWithCorrectPackageName()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalManifest>.That.Matches(m => m.PackageName.Equals(_manifest.PackageName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesInternalManifestWithCorrectProjectid()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalManifest>.That.Matches(m => m.ProjectId.Equals(_manifest.ProjectId))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesInternalManifestWithCorrectUsername()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueService.AddMessageAsync(A<InternalManifest>.That.Matches(m => m.Username.Equals(Username))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesInternalManifestOnCorrectQueue()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _queueServiceFactory.Create(A<string>.That.Matches(s => s.Contains(_manifest.HandlerQueue, StringComparison.OrdinalIgnoreCase))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreInitialProjectUploadAsync_QueuesCallbackWithCorrectProjectId()
    {
        await _testee.StoreInitialProjectUploadAsync(_formFileCollection);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_manifest.ProjectId)))).MustHaveHappenedOnceExactly();
    }
}
