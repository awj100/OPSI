using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class ProjectStateHandler
{
    private const string route = "_admin/projects/{projectId:guid}/{newProjectState}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ProjectsHandler> _logger;
    private readonly IProjectsService _projectsService;

    public ProjectStateHandler(IProjectsService projectsService,
                               IErrorQueueService errorQueueService,
                               ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ProjectsHandler>();
        _projectsService = projectsService;
    }

    [Function(nameof(ProjectStateHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = route)] HttpRequestData req,
                                            Guid projectId,
                                            string newProjectState)
    {
        _logger.LogInformation($"{nameof(ProjectStateHandler)}: {nameof(projectId)} = \"{projectId}\", {nameof(newProjectState)} = \"{newProjectState}\".");

        var validProjectState = ProjectStatesExtensions.GetValidProjectState(newProjectState);
        if (validProjectState.IsNone)
        {
            return req.BadRequest($"Invalid project state: \"{newProjectState}\".");
        }

        try
        {
            await _projectsService.UpdateProjectStateAsync(projectId, validProjectState.Value);

            var response = req.CreateResponse(HttpStatusCode.OK);

            return response;
        }
        catch (ArgumentException exception)
        {
            return req.BadRequest($"Invalid {exception.ParamName}.");
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
            return req.InternalServerError(exception.Message);
        }
    }
}

