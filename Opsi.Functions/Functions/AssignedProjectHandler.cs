using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class AssignedProjectHandler(IProjectsService _projectsService,
                                    IUserProvider _userProvider,
                                    IErrorQueueService _errorQueueService,
                                    ILoggerFactory loggerFactory,
                                    IResponseSerialiser _responseSerialiser)
{
    private const string route = "projects/{projectId:guid}";

    private readonly ILogger<AssignedProjectHandler> _logger = loggerFactory.CreateLogger<AssignedProjectHandler>();

    [Function("AssignedProjectHandler")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = route)] HttpRequestData req,
                                            Guid projectId)
    {
        _logger.LogInformation($"{nameof(projectId)} = {projectId}.");

        try
        {
            var projectWithResources = await _projectsService.GetAssignedProjectAsync(projectId, _userProvider.Username);

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, projectWithResources);

            return response;
        }
        catch (UnassignedToResourceException)
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
