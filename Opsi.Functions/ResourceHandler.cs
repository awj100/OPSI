using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Opsi.Functions.BaseFunctions;
using Opsi.Functions.Dependencies;
using Opsi.Pocos;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;

namespace Opsi.Functions;

public class ResourceHandler : FunctionWithStorageProvisions
{
    public ResourceHandler(IStorageFunctionDependencies storageFunctionDependencies) : base(storageFunctionDependencies)
    {
    }

    [FunctionName(nameof(ResourceHandler))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId:guid}/resource/{*restOfPath}")] HttpRequest req,
        Guid projectId,
        string restOfPath,
        ILogger log,
        ExecutionContext context)
    {
        const string username = "user@test.com";

        log.LogInformation(nameof(ResourceHandler));

        Init(context);

        if (!DoesBodyContainFile(req))
        {
            log.LogError($"Request contained invalid content-type (\"{req.ContentType}\") or no body ({req.Body.Length}).");
            return new BadRequestObjectResult($"Invalid upload - attach the file content as the request's body, with Content-Type header set to \"application/octet-stream\".");
        }

        try
        {
            var fileName = GetFileName(restOfPath);
            string fullPath = GetFullPath(projectId, restOfPath, fileName);
            var currentVersionInfo = await GetVersionInfoAsync(projectId, fullPath);

            if (!CanUserStoreFile(currentVersionInfo, username))
            {
                return new ConflictObjectResult("The resource is currently locked to another user.");
            }

            var nextVersionInfo = currentVersionInfo.GetNextVersionInfo();

            var resourceStorageInfo = new ResourceStorageInfo(
                projectId,
                restOfPath,
                req.Body,
                nextVersionInfo,
                username);

            await StoreFileAsync(resourceStorageInfo, log);

            await StoreRecordOfUploadAsync(resourceStorageInfo);

            if (currentVersionInfo.LockedTo.IsSome)
            {
                await UnlockFileAsync(projectId, restOfPath, username);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, nameof(ResourceHandler), projectId, restOfPath);

            var error = new Error(nameof(ResourceHandler), ex);
            await ErrorQueueService.Value.AddMessageAsync(error);

            throw new Exception(ex.Message);
        }

        return new OkResult();
    }

    private async Task<VersionInfo> GetVersionInfoAsync(Guid projectId, string fullName)
    {
        try
        {
            return await ResourcesService.Value.GetCurrentVersionInfo(projectId, fullName);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to obtain version information for {nameof(projectId)} = \"{projectId}\", {nameof(fullName)} = \"{fullName}\".", ex);
        }
    }

    private async Task<bool> IsNewProjectAsync(Guid projectId)
    {
        return await ProjectsService.Value.IsNewProject(projectId);
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

    private async Task StoreFileAsync(ResourceStorageInfo resourceStorageInfo, ILogger log)
    {
        try
        {
            await StorageService.Value.StoreVersionedFileAsync(resourceStorageInfo);
        }
        catch (Exception ex)
        {
            const string errorPackage = "An error was encountered while storing the file.";
            log.LogError(errorPackage, ex);
            throw new Exception(errorPackage);
        }
    }

    private async Task StoreRecordOfUploadAsync(ResourceStorageInfo resourceStorageInfo)
    {
        await ResourcesService.Value.StoreResourceAsync(resourceStorageInfo);
    }

    private async Task UnlockFileAsync(Guid projectId, string fullName, string username)
    {
        await ResourcesService.Value.UnlockResourceFromUser(projectId, fullName, username);
    }

    private static bool CanUserStoreFile(VersionInfo versionInfo, string username)
    {
        return versionInfo.LockedTo.IsNone || versionInfo.LockedTo.Value == username;
    }

    private static bool DoesBodyContainFile(HttpRequest request)
    {
        const string expectedContentType = "application/octet-stream";

        return request != null
            && request.ContentType == expectedContentType
            && request.Body?.Length > 0;
    }

    private static string GetFileName(string restOfPath)
    {
        var fileName = Path.GetFileName(restOfPath);

        if (String.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception($"Unable to determine file name from path using {nameof(restOfPath)} = \"{restOfPath}\".");
        }

        return fileName;
    }

    private static string GetFullPath(Guid projectId, string restOfPath, string fileName)
    {
        var fullPath = Path.Combine(projectId.ToString(), restOfPath.Substring(0, restOfPath.Length - fileName.Length));

        if (String.IsNullOrWhiteSpace(fullPath))
        {
            throw new Exception($"Unable to build full storage path using {nameof(projectId)} = \"{projectId}\", {nameof(restOfPath)} = \"{restOfPath}\", {nameof(fileName)} = \"{fileName}\".");
        }

        return fullPath;
    }
}
