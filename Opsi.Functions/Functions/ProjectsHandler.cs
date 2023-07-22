using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class ProjectsHandler
{
    private const int defaultPageSize = 50;
    private const string route = "projects/{projectState}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ProjectsHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IUserProvider _userProvider;

    public ProjectsHandler(IProjectsService projectsService,
                           IResponseSerialiser responseSerialiser,
                           IErrorQueueService errorQueueService,
                           IUserProvider userProvider,
                           ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ProjectsHandler>();
        _projectsService = projectsService;
        _responseSerialiser = responseSerialiser;
        _userProvider = userProvider;
    }

    [Function(nameof(ProjectsHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req,
                                            string projectState,
                                            int? optionalPageSize,
                                            string? continuationToken = null)
    {
        var pageSize = optionalPageSize ?? defaultPageSize;

        _logger.LogInformation($"{nameof(ProjectsHandler)}: {nameof(projectState)} = \"{projectState}\".");

        try
        {
            var pageableProjectsResponse = await _projectsService.GetProjectsAsync(projectState, pageSize, continuationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, pageableProjectsResponse);

            return response;
        }
        catch (ArgumentException exception)
        {
            return req.BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
            return req.InternalServerError(exception.Message);
        }
    }
}

