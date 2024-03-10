using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class AssignedProjectsHandler
{
    private const string route = "projects";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<AssignedProjectsHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IUserProvider _userProvider;

    public AssignedProjectsHandler(IProjectsService projectsService,
                                   IResponseSerialiser responseSerialiser,
                                   IErrorQueueService errorQueueService,
                                   IUserProvider userProvider,
                                   ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<AssignedProjectsHandler>();
        _projectsService = projectsService;
        _responseSerialiser = responseSerialiser;
        _userProvider = userProvider;
    }

    [Function(nameof(AssignedProjectsHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req)
    {
        _logger.LogInformation(nameof(AssignedProjectsHandler));

        try
        {
            var pageableUserAssignments = await _projectsService.GetAssignedProjectsAsync(_userProvider.Username.Value);

            var projectWithResources = pageableUserAssignments.GroupBy(userAssignment => userAssignment.ProjectId)
                                                                .Select(projectGrouping => new ProjectWithResources
                                                                {
                                                                    Id = projectGrouping.Key,
                                                                    Name = projectGrouping.First().ProjectName,
                                                                    Resources = projectGrouping.Select(userAssignment => new Resource
                                                                    {
                                                                        AssignedBy = userAssignment.AssignedByUsername,
                                                                        AssignedOnUtc = userAssignment.AssignedOnUtc,
                                                                        AssignedTo = userAssignment.AssigneeUsername,
                                                                        FullName = userAssignment.ResourceFullName,
                                                                        ProjectId = userAssignment.ProjectId
                                                                    }).ToList(),
                                                                    State = ProjectStates.InProgress,
                                                                    Username = pageableUserAssignments.First().AssigneeUsername
                                                                })
                                                                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);

            _responseSerialiser.WriteJsonToBody(response, projectWithResources);

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
