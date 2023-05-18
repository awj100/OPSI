using System.Net.Http.Headers;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;
using Opsi.Services.QueueHandlers.Dependencies;

namespace Opsi.Services.QueueHandlers;

internal class ZippedQueueHandler : IZippedQueueHandler
{
    private readonly AzureStorage.IBlobService _blobService;
    private readonly AzureStorage.IQueueService _callbackQueueService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _log;
    private readonly IProjectsService _projectsService;
    private readonly IUnzipServiceFactory _unzipServiceFactory;

    public ZippedQueueHandler(IConfiguration configuration,
                              IProjectsService projectsService,
                              AzureStorage.IQueueServiceFactory queueServiceFactory,
                              AzureStorage.IBlobService blobService,
                              IUnzipServiceFactory unzipServiceFactory,
                              IHttpClientFactory httpClientFactory,
                              ILoggerFactory loggerFactory)
    {
        _blobService = blobService;
        _callbackQueueService = queueServiceFactory.Create(Constants.QueueNames.Callback);
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _log = loggerFactory.CreateLogger<ZippedQueueHandler>();
        _projectsService = projectsService;
        _unzipServiceFactory = unzipServiceFactory;
    }

    public async Task RetrieveAndHandleUploadAsync(Manifest manifest)
    {
        const string username = "user@test.com";

        var isNewProject = await IsNewProjectAsync(manifest.ProjectId);
        if (!isNewProject)
        {
            var callbackMessage = GetProjectConflictCallbackMessage(manifest);
            await _callbackQueueService.AddMessageAsync(callbackMessage);
            _log.LogWarning(callbackMessage.Status);
            return;
        }

        var project = GetProject(manifest, username);

        await _projectsService.StoreProjectAsync(project);

        using (var zipStream = await _blobService.RetrieveAsync(manifest.GetPackagePathForStore()))
        using (var unzipService = _unzipServiceFactory.Create(zipStream))
        {
            var filePaths = unzipService.GetFilePathsFromPackage();

            await SendResourcesForStoringAsync(filePaths, unzipService, manifest.ProjectId);
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

        await _callbackQueueService.AddMessageAsync(callbackMessage);
    }

    private async Task<HttpResponseMessage> SendResourceForStoringAsync(string hostUrl, string filePath, IUnzipService unzipService, Guid projectId)
    {
        const string mediaTypeHeaderValue = "application/octet-stream";

        var fileName = Path.GetFileName(filePath);

        using (var fileContentsStream = await unzipService.GetContentsAsync(filePath))
        {
            if (fileContentsStream == null)
            {
                throw new Exception($"Stream was null for {filePath}.");
            }

            using (var httpClient = _httpClientFactory.CreateClient())
            using (var content = new MultipartFormDataContent())
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
                using (StreamContent streamContent = new StreamContent(fileContentsStream))
                {
                    content.Add(streamContent, fileName);

                    var url = $"{hostUrl}/projects/{projectId}/resource/{filePath}";
                    return await httpClient.PostAsync(url, content);
                }
            }
        }
    }

    private async Task SendResourcesForStoringAsync(IReadOnlyCollection<string> filePaths, IUnzipService unzipService, Guid projectId)
    {
        const string configKeyHostUrl = "hostUrl";
        var hostUrl = _configuration[configKeyHostUrl];

        foreach (var filePath in filePaths)
        {
            var response = await SendResourceForStoringAsync(hostUrl, filePath, unzipService, projectId);

            await NotifyOfResourceStorageResponseAsync(projectId, filePath, response);
        }
    }

    private static Project GetProject(Manifest manifest, string username)
    {
        return new Project(manifest, username);
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
