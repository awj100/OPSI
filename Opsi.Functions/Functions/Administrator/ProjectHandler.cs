using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class ProjectHandler(IProjectsService _projectsService,
                            IUserProvider _userProvider,
                            IErrorQueueService _errorQueueService,
                            ILoggerFactory loggerFactory,
                            IResponseSerialiser _responseSerialiser)
{
    private const string route = "_admin/projects/{projectId:guid}";

    private readonly IErrorQueueService _errorQueueService = _errorQueueService;
    private readonly ILogger<AssignedProjectHandler> _logger = loggerFactory.CreateLogger<AssignedProjectHandler>();
    private readonly IProjectsService _projectsService = _projectsService;
    private readonly IResponseSerialiser _responseSerialiser = _responseSerialiser;
    private readonly IUserProvider _userProvider = _userProvider;

    [Function("ProjectHandler")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = route)] HttpRequestData req,
                                            Guid projectId)
    {
        _logger.LogInformation($"{nameof(projectId)} = {projectId}.");

        try
        {
            var projectWithResources = await _projectsService.GetProjectAsync(projectId);

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, projectWithResources);

            return response;
        }
        catch (ProjectNotFoundException)
        {
            _logger.LogWarning($"Returning 400 (Bad Request) for project \"{projectId}\".");
            return req.CreateResponse(HttpStatusCode.BadRequest);
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
