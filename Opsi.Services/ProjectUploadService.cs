using Microsoft.Extensions.Logging;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services;

internal class ProjectUploadService : IProjectUploadService
{
    private readonly IBlobService _blobService;
    private readonly ILogger<ProjectUploadService> _log;
    private readonly IManifestService _manifestService;
    private readonly IQueueServiceFactory _queueServiceFactory;
    private readonly IUserProvider _userProvider;
    private readonly IWebhookQueueService _webhookQueueService;

    public int RequiredNumberOfUploadedObjects => 2;

    public ProjectUploadService(IManifestService manifestService,
                                IWebhookQueueService QueueService,
                                IQueueServiceFactory queueServiceFactory,
                                IBlobService blobService,
                                IUserProvider userProvider,
                                ILoggerFactory loggerFactory)
    {
        _manifestService = manifestService;
        _queueServiceFactory = queueServiceFactory;
        _blobService = blobService;
        _userProvider = userProvider;
        _log = loggerFactory.CreateLogger<ProjectUploadService>();
        _webhookQueueService = QueueService;
    }

    public async Task StoreInitialProjectUploadAsync(IFormFileCollection formCollection)
    {
        if (!IsCorrectNumberOfUploads(formCollection))
        {
            _log.LogError($"Request contained {formCollection.Count} file(s).");
            throw new BadRequestException($"Invalid number of files. Expected {ManifestService.ManifestName} and a package.");
        }

        var manifest = await _manifestService.GetManifestAsync(formCollection);

        await StoreNonManifestUploadAsync(formCollection, manifest);

        await QueueManifestAsync(manifest);

        await QueueWebhookMessageAsync(manifest);
    }

    private async Task QueueManifestAsync(Manifest manifest)
    {
        var queueService = GetQueueServiceForManifest(manifest);
        var internalManifest = new InternalManifest(manifest, _userProvider.Username.Value);

        try
        {
            await queueService.AddMessageAsync(internalManifest);
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the manifest.";
            _log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
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
            _log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
    }

    private bool IsCorrectNumberOfUploads(IFormFileCollection formFiles)
    {
        return formFiles.Count == RequiredNumberOfUploadedObjects;
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
                ProjectId = manifest.ProjectId,
                Status = "Project.Uploaded",
                Username = _userProvider.Username.Value
            }, manifest.WebhookSpecification);
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the  message.";
            _log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
    }

    private async Task StoreNonManifestUploadAsync(IFormFileCollection formFiles, Manifest manifest)
    {
        using var nonManifestStream = GetNonManifestFormFile(formFiles);

        try
        {
            await _blobService.StoreAsync(manifest.GetPackagePathForStore(), nonManifestStream);
        }
        catch (Exception ex)
        {
            const string errorPackage = "An error was encountered while storing the package.";
            _log.LogError(errorPackage, ex);
            throw new Exception(errorPackage);
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
            if (String.Equals(formFile.Key, ManifestService.ManifestName))
            {
                continue;
            }

            return formFile.Value;
        }

        throw new BadRequestException("No package was uploaded.");
    }
}
