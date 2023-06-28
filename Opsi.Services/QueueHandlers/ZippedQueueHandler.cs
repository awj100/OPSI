using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Common;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.QueueServices;

namespace Opsi.Services.QueueHandlers;

internal class ZippedQueueHandler : IZippedQueueHandler
{
    private readonly IBlobService _blobService;
    private readonly IWebhookQueueService _QueueService;
    private readonly ILogger _log;
    private readonly IProjectsService _projectsService;
    private readonly IResourceDispatcher _resourceDispatcher;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IUnzipServiceFactory _unzipServiceFactory;

    public ZippedQueueHandler(ISettingsProvider settingsProvider,
                              IProjectsService projectsService,
                              IWebhookQueueService QueueService,
                              IBlobService blobService,
                              IUnzipServiceFactory unzipServiceFactory,
                              IResourceDispatcher resourceDispatcher,
                              ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _QueueService = QueueService;
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
            var webhookMessage = GetProjectConflictWebhookMessage(internalManifest);
            await _QueueService.QueueWebhookMessageAsync(webhookMessage);
            _log.LogWarning(webhookMessage.Status);
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

    private async Task NotifyOfResourceStorageResponseAsync(Guid projectId, string filePath, HttpResponseMessage response, string uri)
    {
        var webhookMessage = GetResourceStorageWebhookMessage(projectId, filePath, response, uri);

        await _QueueService.QueueWebhookMessageAsync(webhookMessage);
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

            await NotifyOfResourceStorageResponseAsync(internalManifest.ProjectId, filePath, response, internalManifest.WebhookUri);
        }
    }

    private static Project GetProject(InternalManifest internalManifest)
    {
        return new Project(internalManifest);
    }

    private static InternalWebhookMessage GetProjectConflictWebhookMessage(Manifest manifest)
    {
        return new InternalWebhookMessage
        {
            ProjectId = manifest.ProjectId,
            RemoteUri = manifest.WebhookUri,
            Status = $"A project with ID \"{manifest.ProjectId}\" already exists."
        };
    }

    private static InternalWebhookMessage GetResourceStorageWebhookMessage(Guid projectId, string filePath, HttpResponseMessage response, string uri)
    {
        return new InternalWebhookMessage
        {
            ProjectId = projectId,
            RemoteUri = uri,
            Status = response.IsSuccessStatusCode
                ? $"Resource stored: {filePath}"
                : $"Resource could not be stored (\"{filePath}\"): {response.ReasonPhrase}"
        };
    }
}
