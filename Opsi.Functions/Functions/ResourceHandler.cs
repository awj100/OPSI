using System.Net;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Common.Exceptions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class ResourceHandler(IResourceService _resourceService,
                             IErrorQueueService _errorQueueService,
                             IUserProvider _userProvider,
                             ILoggerFactory loggerFactory)
{
    private const string route = "projects/{projectId:guid}/resources/{*restOfPath}";

    private readonly ILogger<ResourceHandler> _logger = loggerFactory.CreateLogger<ResourceHandler>();

    [Function(nameof(ResourceHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = route)] HttpRequestData req,
                                             Guid projectId,
                                             string restOfPath)
    {
        if (req.Method == HttpMethod.Post.Method)
        {
            return await HandlePostedFileAsync(req, projectId, restOfPath);
        }

        return await HandleGetFileAsync(req, projectId, restOfPath);
    }

    private async Task<HttpResponseData> HandleGetFileAsync(HttpRequestData req, Guid projectId, string restOfPath)
    {
        _logger.LogInformation($"Request to retrieve \"{projectId}/{restOfPath}\" for user \"{_userProvider.Username}\".");

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

            _logger.LogInformation($"Retrieved \"{projectId}/{restOfPath}\" for user \"{_userProvider.Username}\".");
        }
        catch (UnassignedToResourceException exception)
        {
            _logger.LogWarning($"User {_userProvider.Username} illegally attempted to access \"{projectId}/{restOfPath}\".");
            response = req.CreateResponse(HttpStatusCode.Forbidden);
            await response.WriteStringAsync(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception was thrown while retrieving a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(exception);

            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(exception.Message);
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
        _logger.LogInformation($"Request to store \"{projectId}/{restOfPath}\" for user \"{_userProvider.Username}\".");

        HttpResponseData response;

        if (!DoesBodyContainFile(req))
        {
            _logger.LogError($"Request contained invalid content-type (\"{GetContentTypeOfUpload(req)}\") or no body ({req.Body.Length}).");

            response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync($"Invalid upload - attach the file content as the request's body, with Content-Type header set to \"application/octet-stream\".");
            response.Headers.Add("ContentType", new MediaTypeHeaderValue("text/plain").ToString());

            return response;
        }

        var resourceStorageInfo = new ResourceStorageInfo(projectId,
                                                          restOfPath,
                                                          req.Body);

        try
        {
            await _resourceService.StoreResourceAsync(resourceStorageInfo);

            response = req.CreateResponse(HttpStatusCode.Accepted);

            _logger.LogInformation($"Stored \"{projectId}/{restOfPath}\" for user \"{_userProvider.Username}\".");
        }
        catch (UnassignedToResourceException exception)
        {
            _logger.LogWarning($"User {_userProvider.Username} illegally attempted to store \"{projectId}/{restOfPath}\".");
            response = req.CreateResponse(HttpStatusCode.Forbidden);
            await response.WriteStringAsync(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception was thrown while storing a resource.", projectId, restOfPath);

            await _errorQueueService.ReportAsync(exception);

            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(exception.Message);
        }

        return response;
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
