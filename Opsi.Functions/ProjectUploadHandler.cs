using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Opsi.Functions.BaseFunctions;
using Opsi.Functions.Dependencies;
using Opsi.Pocos;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.Functions;

public class ProjectUploadHandler : FunctionWithStorageProvisions
{
    private const string ManifestName = "manifest.json";
    private readonly IManifestService _manifestService;

    public ProjectUploadHandler(IManifestService manifestService, IStorageFunctionDependencies storageFunctionDependencies) : base(storageFunctionDependencies)
    {
        _manifestService = manifestService;
    }

    [FunctionName(nameof(ProjectUploadHandler))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/upload")] HttpRequest req,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation(nameof(ProjectUploadHandler));

        Init(context);

        var contentType = req.ContentType;
        var files = req.Form.Files;

        if (!IsCorrectNumberOfUploads(req.Form.Files))
        {
            log.LogError($"Request contained {req.Form.Files.Count} file(s).");
            return new BadRequestObjectResult($"Invalid number of files. Expected {ManifestName} and a package.");
        }

        var manifest = await _manifestService.GetManifestAsync(req.Form.Files);
        var project = GetProjectFromManifest(manifest);

        await StorePackageAsync(req.Form.Files, manifest, log);

        await QueueManifestAsync(manifest, log);

        await QueueCallbackMessageAsync(manifest, log);

        return new AcceptedResult();
    }

    private async Task QueueManifestAsync(Manifest manifest, ILogger log)
    {
        var manifestQueueService = GetManifestQueueService(manifest, log);

        try
        {
            await manifestQueueService.AddMessageAsync(manifest);
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the manifest.";
            log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
    }

    private async Task QueueCallbackMessageAsync(Manifest manifest, ILogger log)
    {
        try
        {
            await CallbackQueueService.Value.AddMessageAsync(new CallbackMessage
            {
                ProjectId = manifest.ProjectId,
                Status = "Upload received"
            });
        }
        catch (Exception ex)
        {
            const string errorManifest = "An error was encountered while queuing the callback message.";
            log.LogError(errorManifest, ex);
            throw new Exception(errorManifest);
        }
    }

    private async Task StorePackageAsync(IFormFileCollection formFiles, Manifest manifest, ILogger log)
    {
        using (var packageStream = await GetPackageAsync(formFiles))
        {
            try
            {
                await StorageService.Value.StoreAsync(manifest.GetPackagePathForStore(), packageStream);
            }
            catch (Exception ex)
            {
                const string errorPackage = "An error was encountered while storing the package.";
                log.LogError(errorPackage, ex);
                throw new Exception(errorPackage);
            }
        }
    }

    private static bool IsCorrectNumberOfUploads(IFormFileCollection formFiles)
    {
        const int expectedNumberOfUploads = 2;

        return formFiles.Count == expectedNumberOfUploads;
    }

    private static async Task<Stream> GetPackageAsync(IFormFileCollection formFiles)
    {
        var packageFormFile = GetPackageFormFile(formFiles);

        var content = new MemoryStream();

        await packageFormFile.CopyToAsync(content);

        content.Position = 0;

        return content;
    }

    private static IFormFile GetPackageFormFile(IFormFileCollection formFiles)
    {
        foreach (var formFile in formFiles)
        {
            if (String.Equals(formFile.FileName, ManifestName))
            {
                continue;
            }

            return formFile;
        }

        throw new Exception("No package was uploaded.");
    }

    private static Project GetProjectFromManifest(Manifest manifest)
    {
        return new Project
        {
            CallbackUri = manifest.CallbackUri,
            Id = manifest.ProjectId,
            Name = manifest.PackageName,
            Timestamp = DateTime.Now,
            Username = "user@test.com"
        };
    }
}
