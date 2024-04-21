using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class UserAssignmentHandler
{
    private const string route = "_admin/users/{assigneeUsername}/projects/{projectId:guid}/resource/{*resourceName}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<UserAssignmentHandler> _logger;
    private readonly IProjectsService _projectsService;
    private readonly IUserProvider _userProvider;

    public UserAssignmentHandler(IProjectsService projectsService,
                                 IUserProvider userProvider,
                                 IErrorQueueService errorQueueService,
                                 ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<UserAssignmentHandler>();
        _projectsService = projectsService;
        _userProvider = userProvider;
    }

    [Function(nameof(UserAssignmentHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", "PUT", Route = route)] HttpRequestData req,
                                            string assigneeUsername,
                                            Guid projectId,
                                            string resourceName)
    {
        _logger.LogInformation(nameof(UserAssignmentHandler));

        try
        {
            var userAssignment = GetUserAssignment(assigneeUsername, projectId, resourceName);

            if (req.Method == HttpMethod.Put.Method)
            {
                await _projectsService.AssignUserAsync(userAssignment);

                return req.CreateResponse(HttpStatusCode.Accepted);
            }

            await _projectsService.RevokeUserAsync(userAssignment);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (ArgumentException exception)
        {
            return req.BadRequest($"Invalid {exception.ParamName}.");
        }
        catch(UserAssignmentException exception)
        {
            return req.BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);
            return req.InternalServerError(exception.Message);
        }
    }

    private UserAssignment GetUserAssignment(string assigneeUsername, Guid projectId, string resourceName)
    {
        return new UserAssignment
        {
            AssignedByUsername = _userProvider.Username.Value,
            AssignedOnUtc = DateTime.UtcNow,
            AssigneeUsername = assigneeUsername,
            ProjectId = projectId,
            ResourceFullName = $"{projectId}/{resourceName}"
        };
    }
}
