using System.Net;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class ResourceHandler
{
    private const string route = "projects/{projectId:guid}/resources/{*restOfPath}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ResourceHandler> _logger;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IResourceService _resourceService;
    private readonly IUserProvider _userProvider;

    public ResourceHandler(IResourceService resourceService,
                           IErrorQueueService errorQueueService,
                           IUserProvider userProvider,
                           IResponseSerialiser responseSerialiser,
                           ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ResourceHandler>();
        _responseSerialiser = responseSerialiser;
        _resourceService = resourceService;
        _userProvider = userProvider;
    }

    [Function(nameof(ResourceHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = route)] HttpRequestData req,
                                             Guid projectId,
                                             string restOfPath)
    {
        _logger.LogInformation(nameof(ResourceHandler));

        var accessStatusCode = await HasUserAccessAsync(projectId, restOfPath, _userProvider.Username.Value);
        HttpResponseData response;

        switch (accessStatusCode)
        {
            case HttpStatusCode.InternalServerError:
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"An exception was thrown while determining your access to the resource. This has been logged.");
                return response;

            case HttpStatusCode.Unauthorized:
                response = req.CreateResponse(HttpStatusCode.Unauthorized);
                await response.WriteStringAsync($"You do not have access to \"{restOfPath}\" in project \"{projectId}\".");
                return response;
        }

        if (req.Method == HttpMethod.Post.Method)
        {
            return await HandlePostedFileAsync(req, projectId, restOfPath);
        }

        return await HandleGetFileAsync(req, projectId, restOfPath);
    }

    private async Task<HttpResponseData> HandleGetFileAsync(HttpRequestData req, Guid projectId, string restOfPath)
    {
        HttpResponseData response;

        try
        {
            var resourceContent = await _resourceService.GetResourceContentAsync(projectId, restOfPath);
            if (resourceContent.IsNone)
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteStringAsync($"The requested resource (\"{restOfPath}\") cuold not be found.");

                return response;
            }

            response = req.CreateResponse(HttpStatusCode.OK);
            SetResponseHeaders(response, resourceContent.Value);
            await SetResponseContentAsync(response, resourceContent.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception was thrown while retrieving a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(ex);

            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(ex.Message);
        }

        return response;
    }

/*
    private async Task<HttpResponseData> HandleGetFileAsync(HttpRequestData req, Guid projectId, string restOfPath)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
            var container = blobServiceClient.GetBlobContainerClient("resources");
            var blob = container.GetBlobClient($"{projectId}/{restOfPath}");
            var blobProps = await blob.GetPropertiesAsync();
            var stream = await blob.OpenReadAsync();

            int chunkSize = 10;
            string[] rangeHeaders = (req.Headers.FirstOrDefault(hdr => hdr.Key.ToLower() == "range").Value?.FirstOrDefault() ?? $"range {0}-{chunkSize - 1}").Substring(6).Split("-");
            var start = int.Parse(rangeHeaders[0]);
            var end = String.IsNullOrEmpty(rangeHeaders[1])
                ? (int)Math.Min(start + chunkSize, blobProps.Value.ContentLength - 1)
                : Math.Min(start + chunkSize, int.Parse(rangeHeaders[1]));

            var response = req.CreateResponse(HttpStatusCode.PartialContent);
            response.Headers.Add("Accept-Ranges", "bytes");
            response.Headers.Add("Cache-Control", new CacheControlHeaderValue { NoStore = true }.ToString());
            response.Headers.Add("Connection", "keep-alive");
            response.Headers.Add("Content-Disposition", new ContentDispositionHeaderValue("attachment") { FileName = GetBlobName(blob.Name) }.ToString());
            response.Headers.Add("Content-Length", (end - start + 1).ToString());
            response.Headers.Add("Content-Range", new ContentRangeHeaderValue(start, end, blobProps.Value.ContentLength).ToString());
            response.Headers.Add("Content-Type", blobProps.Value.ContentType);
            response.Headers.Add("Etag", blobProps.Value.ETag.ToString("H"));
            response.Headers.Add("Last-Modified", blobProps.Value.LastModified.ToString("R"));

            var bytes = new byte[chunkSize];
            await stream.ReadAsync(bytes, start, end);
            await response.WriteBytesAsync(bytes);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(ex.Message);

            return response;
        }
    }
*/

    private async Task<HttpResponseData> HandlePostedFileAsync(HttpRequestData req, Guid projectId, string restOfPath)
    {
        if (!DoesBodyContainFile(req))
        {
            _logger.LogError($"Request contained invalid content-type (\"{GetContentTypeOfUpload(req)}\") or no body ({req.Body.Length}).");

            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync($"Invalid upload - attach the file content as the request's body, with Content-Type header set to \"application/octet-stream\".");
            response.Headers.Add("ContentType", new MediaTypeHeaderValue("text/plain").ToString());

            return response;
        }

        var resourceStorageInfo = new ResourceStorageInfo(projectId,
                                                          restOfPath,
                                                          req.Body,
                                                          _userProvider.Username.Value);

        try
        {
            await _resourceService.StoreResourceAsync(resourceStorageInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception was thrown while storing a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(ex);

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(ex.Message);

            return response;
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task<HttpStatusCode> HasUserAccessAsync(Guid projectId, string restOfPath, string username)
    {
        try
        {
            var hasUserAccess = await _resourceService.HasUserAccessAsync(projectId, restOfPath, username);

            if (!hasUserAccess)
            {
                return HttpStatusCode.Unauthorized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception was thrown while determining a user's access to a resource.", projectId, restOfPath, username);

            await _errorQueueService.ReportAsync(ex);

            return HttpStatusCode.InternalServerError;
        }

        return HttpStatusCode.OK;
    }

    private static bool DoesBodyContainFile(HttpRequestData request)
    {
        const string expectedContentType = "application/octet-stream";

        return request != null
            && string.Equals(GetContentTypeOfUpload(request), expectedContentType, StringComparison.OrdinalIgnoreCase)
            && request.Body?.Length > 0;
    }

    private static string GetContentTypeOfUpload(HttpRequestData req)
    {
        if (req.Headers.TryGetValues(Microsoft.Net.Http.Headers.HeaderNames.ContentType, out var contentType))
        {
            return contentType.First();
        }

        return string.Empty;
    }

    private static async Task SetResponseContentAsync(HttpResponseData response, ResourceContent resourceContent)
    {
        await response.WriteBytesAsync(resourceContent.Contents);
    }

    private static void SetResponseHeaders(HttpResponseData response, ResourceContent resourceContent)
    {
        response.Headers.Add("Cache-Control", new CacheControlHeaderValue { NoStore = true }.ToString());
        response.Headers.Add("Content-Disposition", new ContentDispositionHeaderValue("attachment") { FileName = GetFilename(resourceContent.Name) }.ToString());
        response.Headers.Add("Content-Length", resourceContent.Length.ToString());
        response.Headers.Add("Content-Type", resourceContent.ContentType);
        response.Headers.Add("Etag", resourceContent.Etag);
        response.Headers.Add("Last-Modified", resourceContent.LastModified.ToString("R"));

        static string GetFilename(string fullName)
        {
            return fullName.Split("/").LastOrDefault() ?? "filename";
        }
    }
}
