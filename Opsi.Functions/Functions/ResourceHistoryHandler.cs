using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class ResourceHistoryHandler
{
    private const string route = "projects/{projectId:guid}/history/{*restOfPath}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ResourceHandler> _logger;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IResourceService _resourceService;

    public ResourceHistoryHandler(IResourceService resourceService,
                                  IErrorQueueService errorQueueService,
                                  IResponseSerialiser responseSerialiser,
                                  ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ResourceHandler>();
        _responseSerialiser = responseSerialiser;
        _resourceService = resourceService;
    }

    [Function(nameof(ResourceHistoryHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req,
                                             Guid projectId,
                                             string? restOfPath = null)
    {
        _logger.LogInformation(nameof(ResourceHistoryHandler), projectId, restOfPath);

        var response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            if (String.IsNullOrEmpty(restOfPath))
            {
                var groupedHistoricVersions = await _resourceService.GetResourcesHistoryAsync(projectId);
                _responseSerialiser.WriteJsonToBody(response, groupedHistoricVersions);
            }
            else
            {
                var historicVersions = await _resourceService.GetResourceHistoryAsync(projectId, restOfPath);
                _responseSerialiser.WriteJsonToBody(response, historicVersions);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"{nameof(ResourceHistoryHandler)}: An error occurred while querying resource(s) history.", exception);

            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync(exception.Message);

            await _errorQueueService.ReportAsync(exception);
        }

        return response;
    }
}
