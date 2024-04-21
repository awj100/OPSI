using Microsoft.Extensions.Logging;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

internal class ProjectUploadService(IManifestService _manifestService,
                                    IWebhookQueueService _webhookQueueService,
                                    IQueueServiceFactory _queueServiceFactory,
                                    IBlobService _blobService,
                                    IUserProvider _userProvider,
                                    ILoggerFactory loggerFactory) : IProjectUploadService
{
    private readonly ILogger<ProjectUploadService> _log = loggerFactory.CreateLogger<ProjectUploadService>();

    public static int RequiredNumberOfUploadedObjects => 2;

    public async Task StoreInitialProjectUploadAsync(IFormFileCollection formCollection)
    {
        if (!IsCorrectNumberOfUploads(formCollection))
        {
            _log.LogError($"Request contained {formCollection.Count} file(s).");
            throw new BadRequestException($"Invalid number of files. Expected {ManifestService.IncomingManifestName} and a package.");
        }

        var manifest = await _manifestService.ExtractManifestAsync(formCollection);
        var internalManifest = new InternalManifest(manifest, _userProvider.Username.Value);

        await StoreNonManifestUploadAsync(formCollection, internalManifest);

        await QueueManifestAsync(internalManifest);

        await QueueWebhookMessageAsync(manifest);
    }

    private IQueueService GetQueueServiceForManifest(Manifest manifest)
    {
        _log.LogInformation($"{nameof(GetQueueServiceForManifest)}: Resolving manifest queue for \"{manifest.HandlerQueue}\".");

        var manifestQueueName = GetManifestQueueName(manifest);

        try
        {
            return _queueServiceFactory.Create(manifestQueueName);
        }
        catch (Exception ex)
        {
            var errorManifest = $"An error was encountered while resolving a queue service for \"{manifest.HandlerQueue}\".";
            _log.LogError(ex, errorManifest);
            throw new Exception(errorManifest);
        }
    }

    private bool IsCorrectNumberOfUploads(IFormFileCollection formFiles)
    {
        return formFiles.Count == RequiredNumberOfUploadedObjects;
    }

    private async Task QueueManifestAsync(InternalManifest internalManifest)
    {
        var queueService = GetQueueServiceForManifest(internalManifest);

        try
        {
            await queueService.AddMessageAsync(internalManifest);
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the manifest.";
            _log.LogError(ex, errorManifest);
            throw new Exception(errorManifest);
        }
    }

    private async Task QueueWebhookMessageAsync(Manifest manifest)
    {
        if (String.IsNullOrWhiteSpace(manifest.WebhookSpecification?.Uri))
        {
            return;
        }

        try
        {
            await _webhookQueueService.QueueWebhookMessageAsync(new WebhookMessage
            {
                Event = Events.Uploaded,
                Level = Levels.Project,
                Name = manifest.PackageName,
                ProjectId = manifest.ProjectId,
                Username = _userProvider.Username.Value
            }, manifest.WebhookSpecification);
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the  message.";
            _log.LogError(ex, errorManifest);
            throw new Exception(errorManifest);
        }
    }

    private async Task StoreNonManifestUploadAsync(IFormFileCollection formFiles, InternalManifest internalManifest)
    {
        using var nonManifestStream = GetNonManifestFormFile(formFiles);

        try
        {
            await _blobService.StoreResourceAsync(internalManifest.GetNonManifestPathForStore(), nonManifestStream);
        }
        catch (Exception ex)
        {
            const string errorMessage = "An error was encountered while storing the package.";
            _log.LogError(ex, errorMessage);
            throw new Exception(errorMessage);
        }
    }

    private static string GetManifestQueueName(Manifest manifest)
    {
        const string queuePrefix = "manifests";

        return $"{queuePrefix}-{manifest.HandlerQueue.ToLower()}";
    }

    /// <summary>
    /// Gets whatever was uploaded in the form collection alongisde the manifest.
    /// </summary>
    private static Stream GetNonManifestFormFile(IFormFileCollection formFiles)
    {
        foreach (var formFile in formFiles)
        {
            if (String.Equals(formFile.Key, ManifestService.IncomingManifestName))
            {
                continue;
            }

            return formFile.Value;
        }

        throw new BadRequestException("No package was uploaded.");
    }
}
