using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
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
            await _QueueService.QueueWebhookMessageAsync(webhookMessage, internalManifest.WebhookSpecification);
            _log.LogWarning($"{webhookMessage.Level}:{webhookMessage.Event}:{webhookMessage.Name}");
            return;
        }

        var project = GetProject(internalManifest);

        await _projectsService.StoreProjectAsync(project);

        bool areAllResourcesStored = false;

        using (var zipStream = await _blobService.RetrieveAsync(internalManifest.GetPackagePathForStore()))
        using (var unzipService = _unzipServiceFactory.Create(zipStream))
        {
            var filePaths = unzipService.GetFilePathsFromPackage()
                                        .Except(internalManifest.ResourceExclusionPaths)
                                        .ToList();

            areAllResourcesStored = await SendResourcesForStoringAsync(filePaths, unzipService, internalManifest);
        }

        var newState = areAllResourcesStored
            ? ProjectStates.InProgress
            : ProjectStates.Error;

        await SetProjectStateAsync(project.Id, newState);
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

    private async Task NotifyOfResourceStorageResponseAsync(InternalManifest internalManifest, string filePath, HttpResponseMessage response)
    {
        if (String.IsNullOrWhiteSpace(internalManifest.WebhookSpecification?.Uri))
        {
            return;
        }

        var webhookMessage = GetResourceStorageWebhookMessage(internalManifest, filePath, response);

        await _QueueService.QueueWebhookMessageAsync(webhookMessage, internalManifest.WebhookSpecification);
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

    private async Task<bool> SendResourcesForStoringAsync(IReadOnlyCollection<string> filePaths, IUnzipService unzipService, InternalManifest internalManifest)
    {
        const string configKeyHostUrl = "hostUrl";
        var hostUrl = _settingsProvider.GetValue(configKeyHostUrl);
        var areAllResourcesStored = true;

        foreach (var filePath in filePaths)
        {
            var response = await SendResourceForStoringAsync(hostUrl, filePath, unzipService, internalManifest);

            await NotifyOfResourceStorageResponseAsync(internalManifest, filePath, response);

            areAllResourcesStored = areAllResourcesStored && response.IsSuccessStatusCode;
        }

        return areAllResourcesStored;
    }

    private async Task SetProjectStateAsync(Guid projectId, string newState)
    {
        await _projectsService.UpdateProjectStateAsync(projectId, newState);
    }

    private static Project GetProject(InternalManifest internalManifest)
    {
        return new Project(internalManifest, ProjectStates.Initialising);
    }

    private static WebhookMessage GetProjectConflictWebhookMessage(InternalManifest internalManifest)
    {
        return new WebhookMessage
        {
            Event = Events.AlreadyExists,
            Level = Levels.Project,
            Name = internalManifest.PackageName,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        };
    }

    private static WebhookMessage GetResourceStorageWebhookMessage(InternalManifest internalManifest, string filePath, HttpResponseMessage response)
    {
        return new WebhookMessage
        {
            Event = Events.Stored,
            Level = Levels.Resource,
            Name = filePath,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        };
    }
}
