using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions;

public class UserAssignmentsHandler
{
    private const string route = "userAssignments";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<AssignedProjectsHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IUserProvider _userProvider;

    public UserAssignmentsHandler(IProjectsService projectsService,
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

    [Function(nameof(UserAssignmentsHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req)
    {
        _logger.LogInformation(nameof(AssignedProjectsHandler));

        try
        {
            var pageableProjectsResponse = await _projectsService.GetAssignedProjectsAsync(_userProvider.Username);

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
