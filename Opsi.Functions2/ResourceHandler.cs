using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Services;

namespace Opsi.Functions2;

public class ResourceHandler
{
    private const string route = "projects/{projectId:guid}/resource/{*restOfPath}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ResourceHandler> _logger;
    private readonly IResourceService _resourceService;

    public ResourceHandler(IResourceService resourceService,
                           IErrorQueueService errorQueueService,
                           ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ResourceHandler>();
        _resourceService = resourceService;
    }

    [Function(nameof(ResourceHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = route)] HttpRequestData req,
                                         Guid projectId,
                                         string restOfPath)
    {
        const string username = "user@test.com";

        _logger.LogInformation(nameof(ResourceHandler));

        if (!DoesBodyContainFile(req))
        {
            _logger.LogError($"Request contained invalid content-type (\"{GetContentType(req)}\") or no body ({req.Body.Length}).");
            return req.BadRequest($"Invalid upload - attach the file content as the request's body, with Content-Type header set to \"application/octet-stream\".");
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
            _logger.LogError(ex, "An exception was thrown while storing a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(ex);

            throw new Exception(ex.Message);
        }

        return req.Ok();
    }

    private static bool DoesBodyContainFile(HttpRequestData request)
    {
        const string expectedContentType = "application/octet-stream";

        return request != null
            && String.Equals(GetContentType(request), expectedContentType, StringComparison.OrdinalIgnoreCase)
            && request.Body?.Length > 0;
    }

    private static string GetContentType(HttpRequestData req)
    {
        if (req.Headers.TryGetValues(Microsoft.Net.Http.Headers.HeaderNames.ContentType, out var contentType))
        {
            return contentType.First();
        }

        return String.Empty;
    }
}

