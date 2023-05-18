using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Services;

namespace Opsi.Functions;

public class ResourceHandler
{
    private const string route = "projects/{projectId:guid}/resource/{*restOfPath}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ResourceHandler> _log;
    private readonly IResourceService _resourceService;

    public ResourceHandler(IResourceService resourceService,
                           IErrorQueueService errorQueueService,
                           ILogger<ResourceHandler> log)
    {
        _errorQueueService = errorQueueService;
        _log = log;
        _resourceService = resourceService;
    }

    [FunctionName(nameof(ResourceHandler))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = route)] HttpRequest req,
                                         Guid projectId,
                                         string restOfPath)
    {
        const string username = "user@test.com";

        _log.LogInformation(nameof(ResourceHandler));

        if (!DoesBodyContainFile(req))
        {
            _log.LogError($"Request contained invalid content-type (\"{req.ContentType}\") or no body ({req.Body.Length}).");
            return new BadRequestObjectResult($"Invalid upload - attach the file content as the request's body, with Content-Type header set to \"application/octet-stream\".");
        }

        var resourceStorageInfo = new ResourceStorageInfo(projectId,
                                                          restOfPath,
                                                          req.Body,
                                                          username);

        try
        {
            await _resourceService.StoreResourceAsync(resourceStorageInfo);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "An exception was thrown while storing a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(ex);

            throw new Exception(ex.Message);
        }

        return new OkResult();
    }

    private static bool DoesBodyContainFile(HttpRequest request)
    {
        const string expectedContentType = "application/octet-stream";

        return request != null
            && request.ContentType == expectedContentType
            && request.Body?.Length > 0;
    }
}
