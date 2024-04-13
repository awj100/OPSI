using Microsoft.Extensions.Logging;
using Opsi.AzureStorage;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueHandlers.Dependencies;
using Opsi.Services.QueueServices;

namespace Opsi.Services.QueueHandlers;

internal class ZippedQueueHandler(ISettingsProvider _settingsProvider,
                                  IProjectsService _projectsService,
                                  IWebhookQueueService _queueService,
                                  IBlobService _blobService,
                                  IUnzipServiceFactory _unzipServiceFactory,
                                  IResourceDispatcher _resourceDispatcher,
                                  IUserInitialiser _userInitialiser,
                                  IErrorQueueService _errorQueueService,
                                  ILoggerFactory loggerFactory) : IZippedQueueHandler
{
    private readonly ILogger _log = loggerFactory.CreateLogger<ZippedQueueHandler>();

    public async Task RetrieveAndHandleUploadAsync(InternalManifest internalManifest)
    {
        _userInitialiser.SetUsername(internalManifest.Username, true);

        var isNewProject = await IsNewProjectAsync(internalManifest.ProjectId);
        if (!isNewProject)
        {
            await HandleProjectConflictAsync(internalManifest);
            return;
        }

        await _projectsService.InitProjectAsync(internalManifest);

        bool areAllResourcesStored = false;

        using (var zipStream = await _blobService.RetrieveContentAsync(internalManifest.GetNonManifestPathForStore()))
        {
            if (zipStream == null)
            {
                await NotifyOfResourceStorageErrorAsync(internalManifest, "No resources could be found for storing.");
                return;
            }

            using (var unzipService = _unzipServiceFactory.Create(zipStream))
            {
                var filePaths = unzipService.GetFilePathsFromPackage()
                                            .Except(internalManifest.ResourceExclusionPaths)
                                            .ToList();

                areAllResourcesStored = await SendResourcesForStoringAsync(filePaths, unzipService, internalManifest);
            }
        }

        var newState = areAllResourcesStored
            ? ProjectStates.InProgress
            : ProjectStates.Error;

        await SetProjectStateAsync(internalManifest.ProjectId, newState);
    }

    private async Task HandleProjectConflictAsync(InternalManifest internalManifest)
    {
        var webhookMessage = GetProjectConflictWebhookMessage(internalManifest);
        await _queueService.QueueWebhookMessageAsync(webhookMessage, internalManifest?.WebhookSpecification);
        _log.LogWarning($"{webhookMessage.Level}:{webhookMessage.Event}:{webhookMessage.Name}");
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

    private async Task NotifyOfResourceStorageErrorAsync(InternalManifest internalManifest, string errorMessage)
    {
        if (String.IsNullOrWhiteSpace(internalManifest.WebhookSpecification?.Uri))
        {
            return;
        }

        var webhookMessage = GetResourcesNotFoundWebhookMessage(internalManifest);

        await _errorQueueService.ReportAsync(new Exception(errorMessage), LogLevel.Warning, $"{nameof(ZippedQueueHandler)}.{nameof(RetrieveAndHandleUploadAsync)}");
        await _queueService.QueueWebhookMessageAsync(webhookMessage, internalManifest.WebhookSpecification);
    }

    private async Task NotifyOfResourceStorageResponseAsync(InternalManifest internalManifest, string filePath, bool isStored)
    {
        if (String.IsNullOrWhiteSpace(internalManifest.WebhookSpecification?.Uri))
        {
            return;
        }

        var webhookMessage = GetResourceStorageWebhookMessage(internalManifest, filePath, isStored);

        await _queueService.QueueWebhookMessageAsync(webhookMessage, internalManifest.WebhookSpecification);
    }

    private async Task<bool> SendResourceForStoringAsync(string hostUrl,
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

            const bool isAdministrator = true;  // Assume that only administrators can upload projects.

            var response = await _resourceDispatcher.DispatchAsync(hostUrl,
                                                                   internalManifest.ProjectId,
                                                                   filePath,
                                                                   fileContentsStream,
                                                                   internalManifest.Username,
                                                                   isAdministrator);

            return response.IsSuccessStatusCode;
        }
    }

    private async Task<bool> SendResourcesForStoringAsync(IReadOnlyCollection<string> filePaths, IUnzipService unzipService, InternalManifest internalManifest)
    {
        const string configKeyHostUrl = "hostUrl";
        var hostUrl = _settingsProvider.GetValue(configKeyHostUrl);
        var areAllResourcesStored = true;

        foreach (var filePath in filePaths)
        {
            var isStored = await SendResourceForStoringAsync(hostUrl, filePath, unzipService, internalManifest);

            await NotifyOfResourceStorageResponseAsync(internalManifest, filePath, isStored);

            areAllResourcesStored = areAllResourcesStored && isStored;
        }

        return areAllResourcesStored;
    }

    private async Task SetProjectStateAsync(Guid projectId, string newState)
    {
        await _projectsService.UpdateProjectStateAsync(projectId, newState);
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

    private static WebhookMessage GetResourceNotFoundWebhookMessage(InternalManifest internalManifest, string resourceName)
    {
        return new WebhookMessage
        {
            Event = Events.StoreFailure,
            Level = Levels.Resource,
            Name = resourceName,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        };
    }

    private static WebhookMessage GetResourcesNotFoundWebhookMessage(InternalManifest internalManifest)
    {
        return new WebhookMessage
        {
            Event = Events.StoreFailure,
            Level = Levels.Project,
            Name = internalManifest.PackageName,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        };
    }

    private static WebhookMessage GetResourceStorageWebhookMessage(InternalManifest internalManifest, string filePath, bool isStored)
    {
        return isStored
        ? new WebhookMessage
        {
            Event = Events.Stored,
            Level = Levels.Resource,
            Name = filePath,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        }
        : new WebhookMessage
        {
            Event = Events.StoreFailure,
            Level = Levels.Resource,
            Name = filePath,
            ProjectId = internalManifest.ProjectId,
            Username = internalManifest.Username
        };
    }
}
