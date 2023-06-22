using System.IO.Compression;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;
using Opsi.Services.QueueHandlers;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs.QueueHandlers;

[TestClass]
public class ZippedQueueHandlerSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private InternalManifest _manifest;
    private IReadOnlyCollection<string> _nonManifestContentFilePaths;
    private Stream _nonManifestStream;
    private IBlobService _blobService;
    private ICallbackQueueService _callbackQueueService;
    private IProjectsService _projectsService;
    private IResourceDispatcher _resourceDispatcher;
    private ISettingsProvider _settingsProvider;
    private IUnzipService _unzipService;
    private IUnzipServiceFactory _unzipServiceFactory;
    private ZippedQueueHandler _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _manifest = new InternalManifest
        {
            ResourceExclusionPaths = new List<string> { GetTestFilePath(1) },
            ProjectId = Guid.NewGuid()
        };
        _nonManifestContentFilePaths = new List<string>
        {
            GetTestFilePath(0),
            GetTestFilePath(1),
            GetTestFilePath(2)
        };
        _nonManifestStream = GetNonManifestArchiveStream(_nonManifestContentFilePaths);

        _blobService = A.Fake<IBlobService>();
        _callbackQueueService = A.Fake<ICallbackQueueService>();
        _projectsService = A.Fake<IProjectsService>();
        _resourceDispatcher = A.Fake<IResourceDispatcher>();
        _settingsProvider = A.Fake<ISettingsProvider>();
        _unzipService = A.Fake<IUnzipService>();
        _unzipServiceFactory = A.Fake<IUnzipServiceFactory>();

        A.CallTo(() => _blobService.RetrieveAsync(A<string>.That.Matches(s => s.Contains(_manifest.ProjectId.ToString())))).Returns(_nonManifestStream);
        A.CallTo(() => _unzipServiceFactory.Create(_nonManifestStream)).Returns(_unzipService);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        var loggerFactory = new NullLoggerFactory();

        _testee = new ZippedQueueHandler(_settingsProvider,
                                         _projectsService,
                                         _callbackQueueService,
                                         _blobService,
                                         _unzipServiceFactory,
                                         _resourceDispatcher,
                                         loggerFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _nonManifestStream?.Dispose();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdAlreadyRecorded_DoesNotStoreProject()
    {
        const bool isNewProject = false;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _projectsService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdAlreadyRecorded_DoesNotStoreAnyResources()
    {
        const bool isNewProject = false;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                         A<Guid>._,
                                                         A<string>._,
                                                         A<Stream>._,
                                                         A<string>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdAlreadyRecorded_DoesNotSendAnyResourceStoredCallbacks()
    {
        const bool isNewProject = false;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_manifest.ProjectId)
                                                                                                      && cm.Status.Contains("Resource stored"))))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdAlreadyRecorded_QueuesCallbackWithConflictMessage()
    {
        const bool isNewProject = false;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_manifest.ProjectId)
                                                                                                      && cm.Status.Equals($"A project with ID \"{_manifest.ProjectId}\" already exists."))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_StoresProject()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _projectsService.StoreProjectAsync(A<Project>.That.Matches(p => p.Id.Equals(_manifest.ProjectId)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_RetrievesNonManifestUpload()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _blobService.RetrieveAsync(A<string>.That.Matches(s => s.Contains(_manifest.ProjectId.ToString())))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_StoresAllNonExcludedResources()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        foreach(var nonExcludedFilePath in nonExcludedFilePaths)
        {
            A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                             A<Guid>._,
                                                             nonExcludedFilePath,
                                                             A<Stream>._,
                                                             A<string>._)).MustHaveHappenedOnceExactly();
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_DoesNotStoreExcludedResources()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        foreach (var excludedFilePath in _manifest.ResourceExclusionPaths)
        {
            A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                             A<Guid>._,
                                                             excludedFilePath,
                                                             A<Stream>._,
                                                             A<string>._)).MustNotHaveHappened();
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_StoresArchivedStreamCorrespondingToFilePath()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        var fileContentStreams = new Dictionary<string, Stream>();

        try
        {
            foreach (var nonExcludedFilePath in nonExcludedFilePaths)
            {
                var fileSpecificStream = new MemoryStream();
                fileContentStreams.Add(nonExcludedFilePath, fileSpecificStream);

                A.CallTo(() => _unzipService.GetContentsAsync(nonExcludedFilePath)).Returns(fileSpecificStream);
            }

            await _testee.RetrieveAndHandleUploadAsync(_manifest);

            foreach (var nonExcludedFilePath in nonExcludedFilePaths)
            {
                var fileSpecificStream = fileContentStreams[nonExcludedFilePath];

                A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                                 A<Guid>._,
                                                                 A<string>._,
                                                                 fileSpecificStream,
                                                                 A<string>._)).MustHaveHappenedOnceExactly();
            }
        }
        finally
        {
            foreach(var key in fileContentStreams.Keys)
            {
                await fileContentStreams[key].DisposeAsync();
            }
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_DoesNotRetrieveStreamCorrespondingToExcludedFilePath()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        foreach (var excludedFilePath in _manifest.ResourceExclusionPaths)
        {
            A.CallTo(() => _unzipService.GetContentsAsync(excludedFilePath)).MustNotHaveHappened();
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_StoresResourcesUsingManifestProjectId()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                         _manifest.ProjectId,
                                                         A<string>._,
                                                         A<Stream>._,
                                                         A<string>._)).MustHaveHappened();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_StoresResourcesUsingConfiguredHostUrl()
    {
        const string configKeyHostUrl = "hostUrl";
        const string configuredHostUrl = "test host URL";
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);
        A.CallTo(() => _settingsProvider.GetValue(A<string>.That.Matches(s => s.Equals(configKeyHostUrl)),
                                                  false,
                                                  A<string>._)).Returns(configuredHostUrl);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        A.CallTo(() => _resourceDispatcher.DispatchAsync(configuredHostUrl,
                                                         A<Guid>._,
                                                         A<string>._,
                                                         A<Stream>._,
                                                         A<string>._)).MustHaveHappened();
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_SendsCallbackForAllNonExcludedResources()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        foreach (var nonExcludedFilePath in nonExcludedFilePaths)
        {
            A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Equals($"Resource stored: {nonExcludedFilePath}")))).MustHaveHappenedOnceExactly();
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_SendsCallbackForAllNonExcludedResourcesWithCorrectProjectId()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.ProjectId.Equals(_manifest.ProjectId)))).MustHaveHappenedANumberOfTimesMatching(times => times == nonExcludedFilePaths.Count);
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenProjectIdIsNew_DoesNotSendCallbackForExcludedResources()
    {
        const bool isNewProject = true;
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        foreach (var excludedFilePath in _manifest.ResourceExclusionPaths)
        {
            A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.Contains(excludedFilePath)))).MustNotHaveHappened();
        }
    }

    [TestMethod]
    public async Task RetrieveAndHandleUploadAsync_WhenResourceDispatcherReportsNotStored_SendsCallbackForAllNonExcludedResourcesWithCorrectProjectId()
    {
        const bool isNewProject = true;
        var nonSuccessResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        A.CallTo(() => _projectsService.IsNewProjectAsync(_manifest.ProjectId)).Returns(isNewProject);
        A.CallTo(() => _unzipService.GetFilePathsFromPackage()).Returns(_nonManifestContentFilePaths);
        A.CallTo(() => _resourceDispatcher.DispatchAsync(A<string>._,
                                                         A<Guid>._,
                                                         A<string>._,
                                                         A<Stream>._,
                                                         A<string>._)).Returns(nonSuccessResponse);

        await _testee.RetrieveAndHandleUploadAsync(_manifest);

        var nonExcludedFilePaths = _nonManifestContentFilePaths.Except(_manifest.ResourceExclusionPaths).ToList();

        foreach(var nonExcludedFilePath in nonExcludedFilePaths)
        {
            A.CallTo(() => _callbackQueueService.QueueCallbackAsync(A<CallbackMessage>.That.Matches(cm => cm.Status.StartsWith($"Resource could not be stored (\"{nonExcludedFilePath}\")")))).MustHaveHappenedOnceExactly();
        }
    }

    private static Stream GetNonManifestArchiveStream(IReadOnlyCollection<string> filePaths)
    {
        static void AddFile(ZipArchive archive, string filePath)
        {
            ZipArchiveEntry readmeEntry = archive.CreateEntry(filePath);
            using StreamWriter writer = new(readmeEntry.Open());
            writer.WriteLine($"{filePath} contents");
            writer.WriteLine("========================");
        }

        var stream = new MemoryStream();

        using (ZipArchive archive = new(stream, ZipArchiveMode.Create))
        {
            foreach (var filePath in filePaths)
            {
                AddFile(archive, filePath);
            }
        }

        return stream;
    }

    private static string GetTestFilePath(int fileIndex)
    {
        return $"testfile_{fileIndex}.txt";
    }
}
