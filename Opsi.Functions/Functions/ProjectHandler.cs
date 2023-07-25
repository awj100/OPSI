using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class ProjectHandler
{
    private const string route = "projects/{projectId:guid}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ProjectHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IResponseSerialiser _responseSerialiser;

    public ProjectHandler(IProjectsService projectsService,
                          IErrorQueueService errorQueueService,
                          ILoggerFactory loggerFactory,
                          IResponseSerialiser responseSerialiser)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ProjectHandler>();
        _projectsService = projectsService;
        _responseSerialiser = responseSerialiser;
    }

    [Function("ProjectHandler")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = route)] HttpRequestData req,
                                            Guid projectId)
    {
        _logger.LogInformation($"{nameof(projectId)} = {projectId}.");

        try
        {
            var projectWithResources = await _projectsService.GetProjectAsync(projectId);

            if (projectWithResources == null)
            {
                _logger.LogWarning($"Returning 400 (BadRequest) for project \"{projectId}\".");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, projectWithResources);

            return response;
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(exception.Message);
            return response;
        }
    }
}
