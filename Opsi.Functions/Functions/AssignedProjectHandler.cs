using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class AssignedProjectHandler
{
    private const string route = "projects/{projectId:guid}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<AssignedProjectHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IUserProvider _userProvider;

    public AssignedProjectHandler(IProjectsService projectsService,
                                  IUserProvider userProvider,
                                  IErrorQueueService errorQueueService,
                                  ILoggerFactory loggerFactory,
                                  IResponseSerialiser responseSerialiser)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<AssignedProjectHandler>();
        _projectsService = projectsService;
        _responseSerialiser = responseSerialiser;
        _userProvider = userProvider;
    }

    [Function("AssignedProjectHandler")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = route)] HttpRequestData req,
                                            Guid projectId)
    {
        _logger.LogInformation($"{nameof(projectId)} = {projectId}.");

        try
        {
            var projectWithResources = await _projectsService.GetAssignedProjectAsync(projectId, _userProvider.Username.Value);

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, projectWithResources);

            return response;
        }
        catch(UnassignedToProjectException)
        {
            _logger.LogWarning($"Returning 401 (Unauthorized) for project \"{projectId}\".");
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }
        catch (ProjectNotFoundException)
        {
            _logger.LogWarning($"Returning 400 (Bad Request) for project \"{projectId}\".");
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (ProjectStateException)
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
