using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ResourceServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private readonly Guid _projectId = Guid.NewGuid();
    private const string RestOfPath = "rest/of/path";
    private const string Username = "test@username";
    private const int VersionIndex = 123;
    private VersionInfo _versionInfo = new(VersionIndex);

    private IBlobService _blobService;
    private ICallbackQueueService _callbackQueueService;
    private ILogger<ResourceService> _logger;
    private ILoggerFactory _loggerFactory;
    private IResourcesService _resourcesService;
    private ResourceStorageInfo _resourceStorageInfo;
    private ResourceService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _blobService = A.Fake<IBlobService>();
        _callbackQueueService = A.Fake<ICallbackQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _resourcesService = A.Fake<IResourcesService>();

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value))))
            .Returns(_versionInfo);

        _resourceStorageInfo = new ResourceStorageInfo(_projectId,
                                                       RestOfPath,
                                                       new MemoryStream(),
                                                       Username);

        _testee = new ResourceService(_resourcesService,
                                      _blobService,
                                      _callbackQueueService,
                                      _loggerFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _resourceStorageInfo.ContentStream?.Dispose();
    }

    [TestMethod]
    public async Task StoreResourceAsync_DeterminesVersionInfo()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsLockedToAnotherUser_ThrowsException()
    {
        _versionInfo.LockedTo = Option<string>.Some($"another_{Username}");

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value))))
            .Returns(_versionInfo);

        await _testee.Invoking(y => y.StoreResourceAsync(_resourceStorageInfo))
                     .Should()
                     .ThrowAsync<ResourceLockConflictException>();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsLockedToSameUser_UnlocksResource()
    {
        _versionInfo.LockedTo = Option<string>.Some(Username);

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value))))
            .Returns(_versionInfo);

        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.UnlockResourceFromUser(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                                A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value)),
                                                                A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.Username))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_StoredResourceHasIncrementedVersionIndex()
    {
        const int incrementedVersionIndex = VersionIndex + 1;

        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<ResourceStorageInfo>.That.Matches(rsi => rsi.VersionInfo.Index == incrementedVersionIndex)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_BlobVersionIsStoredInResourcesTable()
    {
        var blobVersion = Guid.NewGuid().ToString();

        A.CallTo(() => _blobService.StoreVersionedFileAsync(A<ResourceStorageInfo>._)).Returns(blobVersion);

        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<ResourceStorageInfo>.That.Matches(rsi => rsi.VersionId == blobVersion))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_CallbackMessageIsQueued()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_projectId)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenStoringBlobVersionFails_BlobIsDeleted()
    {
        var blobVersion = Guid.NewGuid().ToString();

        A.CallTo(() => _blobService.StoreVersionedFileAsync(A<ResourceStorageInfo>._)).Returns(blobVersion);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<ResourceStorageInfo>._)).Throws<Exception>();

        await _testee.Invoking(y => y.StoreResourceAsync(_resourceStorageInfo))
                     .Should()
                     .ThrowAsync<Exception>();

        A.CallTo(() => _blobService.DeleteAsync(A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value)))).MustHaveHappenedOnceExactly();
    }
}
