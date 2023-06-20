using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;
using Opsi.Services.QueueHandlers.Dependencies;

namespace Opsi.Services.QueueHandlers;

internal class ZippedQueueHandler : IZippedQueueHandler
{
    private readonly IBlobService _blobService;
    private readonly ICallbackQueueService _callbackQueueService;
    private readonly ILogger _log;
    private readonly IProjectsService _projectsService;
    private readonly IResourceDispatcher _resourceDispatcher;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IUnzipServiceFactory _unzipServiceFactory;

    public ZippedQueueHandler(ISettingsProvider settingsProvider,
                              IProjectsService projectsService,
                              ICallbackQueueService callbackQueueService,
                              IBlobService blobService,
                              IUnzipServiceFactory unzipServiceFactory,
                              IResourceDispatcher resourceDispatcher,
                              ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _callbackQueueService = callbackQueueService;
        _log = loggerFactory.CreateLogger<ZippedQueueHandler>();
        _projectsService = projectsService;
        _resourceDispatcher = resourceDispatcher;
        _settingsProvider = settingsProvider;
        _unzipServiceFactory = unzipServiceFactory;
    }

    public async Task RetrieveAndHandleUploadAsync(InternalManifest internalManifest)
    {
        var isNewProject = await IsNewProjectAsync(internalManifest.ProjectId);
        if (!isNewProject)
        {
            var callbackMessage = GetProjectConflictCallbackMessage(internalManifest);
            await _callbackQueueService.QueueCallbackAsync(callbackMessage);
            _log.LogWarning(callbackMessage.Status);
            return;
        }

        var project = GetProject(internalManifest);

        await _projectsService.StoreProjectAsync(project);

        using (var zipStream = await _blobService.RetrieveAsync(internalManifest.GetPackagePathForStore()))
        using (var unzipService = _unzipServiceFactory.Create(zipStream))
        {
            var filePaths = unzipService.GetFilePathsFromPackage()
                                        .Except(internalManifest.ResourceExclusionPaths)
                                        .ToList();

            await SendResourcesForStoringAsync(filePaths, unzipService, internalManifest);
        }
    }

    private async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        try
        {
            return await _projectsService.IsNewProjectAsync(projectId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to determine if \"{projectId}\" represents a new project.", ex);
        }
    }

    private async Task NotifyOfResourceStorageResponseAsync(Guid projectId, string filePath, HttpResponseMessage response)
    {
        var callbackMessage = GetResourceStorageCallbackMessage(projectId, filePath, response);

        await _callbackQueueService.QueueCallbackAsync(callbackMessage);
    }

    private async Task<HttpResponseMessage> SendResourceForStoringAsync(string hostUrl,
                                                                        string filePath,
                                                                        IUnzipService unzipService,
                                                                        InternalManifest internalManifest)
    {
        using (var fileContentsStream = await unzipService.GetContentsAsync(filePath))
        {
            if (fileContentsStream == null)
            {
                throw new Exception($"Stream was null for {filePath}.");
            }

            return await _resourceDispatcher.DispatchAsync(hostUrl,
                                                           internalManifest.ProjectId,
                                                           filePath,
                                                           fileContentsStream,
                                                           internalManifest.Username);
        }
    }

    private async Task SendResourcesForStoringAsync(IReadOnlyCollection<string> filePaths, IUnzipService unzipService, InternalManifest internalManifest)
    {
        const string configKeyHostUrl = "hostUrl";
        var hostUrl = _settingsProvider.GetValue(configKeyHostUrl);

        foreach (var filePath in filePaths)
        {
            var response = await SendResourceForStoringAsync(hostUrl, filePath, unzipService, internalManifest);

            await NotifyOfResourceStorageResponseAsync(internalManifest.ProjectId, filePath, response);
        }
    }

    private static Project GetProject(InternalManifest internalManifest)
    {
        return new Project(internalManifest);
    }

    private static CallbackMessage GetProjectConflictCallbackMessage(Manifest manifest)
    {
        return new CallbackMessage
        {
            ProjectId = manifest.ProjectId,
            Status = $"A project with ID \"{manifest.ProjectId}\" already exists."
        };
    }

    private static CallbackMessage GetResourceStorageCallbackMessage(Guid projectId, string filePath, HttpResponseMessage response)
    {
        return new CallbackMessage
        {
            ProjectId = projectId,
            Status = response.IsSuccessStatusCode
                ? $"Resource stored: {filePath}"
                : $"Resource could not be stored (\"{filePath}\"): {response.ReasonPhrase}"
        };
    }
}
