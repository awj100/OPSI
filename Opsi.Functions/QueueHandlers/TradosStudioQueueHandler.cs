using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Opsi.Functions.Dependencies;
using Opsi.Pocos;
using Opsi.Services.TableEntities;
using Opsi.TradosStudio;

namespace Opsi.Functions.PackageHandlers;

public class TradosStudioQueueHandler : QueueHandlerFunctionBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TradosStudioQueueHandler(Func<Stream, IPackageService> packageServiceFactory,
        IHttpClientFactory httpClientFactory,
        IStorageFunctionDependencies storageFunctionDependencies) : base(packageServiceFactory, storageFunctionDependencies)
    {
        _httpClientFactory = httpClientFactory;
    }

    [FunctionName(nameof(TradosStudioQueueHandler))]
    public async Task Run(
        [QueueTrigger($"manifests-{Constants.QueueHandlerNames.TradosStudio}", Connection = "AzureWebJobsStorage")]Manifest manifest,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation($"{nameof(TradosStudioQueueHandler)} triggered for \"{manifest.PackageName}\".");

        Init(context);

        try
        {
            var isNewProject = await IsNewProjectAsync(manifest.ProjectId);
            if (!isNewProject)
            {
                var callbackMessage = GetProjectConflictCallbackMessage(manifest);
                await CallbackQueueService.Value.AddMessageAsync(callbackMessage);
                log.LogWarning(callbackMessage.Status);
                return;
            }

            var project = new Project
            {
                CallbackUri = manifest.CallbackUri,
                Id = manifest.ProjectId,
                Name = manifest.PackageName,
                Username = "user@test.com"
            };

            using (var packageStream = await StorageService.Value.RetrieveAsync(manifest.GetPackagePathForStore()))
            using (var packageService = PackageServiceFactory(packageStream))
            {
                var filePaths = packageService.GetFilePathsFromPackage();

                await SendResourcesForStoringAsync(filePaths, packageService, manifest.ProjectId);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, nameof(TradosStudioQueueHandler));

            var error = new Error(nameof(TradosStudioQueueHandler), ex);
            await ErrorQueueService.Value.AddMessageAsync(error);

            throw new Exception(ex.Message);
        }
    }

    private async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        try
        {
            return await ProjectsService.Value.IsNewProject(projectId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to determine if \"{projectId}\" represents a new project.", ex);
        }
    }

    private async Task NotifyOfResourceStoragResponseAsync(Guid projectId, string filePath, HttpResponseMessage response)
    {
        var callbackMessage = GetResourceStorageCallbackMessage(projectId, filePath, response);

        await CallbackQueueService.Value.AddMessageAsync(callbackMessage);
    }

    private async Task SendResourceForStoringAsync(string hostUrl, string filePath, IPackageService packageService, Guid projectId)
    {
        const string mediaTypeHeaderValue = "application/octet-stream";

        var fileName = Path.GetFileName(filePath);

        HttpResponseMessage response;

        using (var fileContentsStream = await packageService.GetContentsAsync(filePath))
        using (var httpClient = _httpClientFactory.CreateClient())
        using (var content = new MultipartFormDataContent())
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            content.Add(new StreamContent(fileContentsStream), fileName);

            var url = $"{hostUrl}/projects/{projectId}/resource/{filePath}";
            response = await httpClient.PostAsync(url, content);
        }

        await NotifyOfResourceStoragResponseAsync(projectId, filePath, response);
    }

    private async Task SendResourcesForStoringAsync(IReadOnlyCollection<string> filePaths, IPackageService packageService, Guid projectId)
    {
        const string configKeyHostUrl = "hostUrl";
        var hostUrl = Configuration[configKeyHostUrl];

        foreach (var filePath in filePaths)
        {
            await SendResourceForStoringAsync(hostUrl, filePath, packageService, projectId);
        }
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
