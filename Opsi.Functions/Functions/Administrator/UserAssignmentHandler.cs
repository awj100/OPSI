using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class UserAssignmentHandler(IProjectsService _projectsService,
                                   IUserProvider _userProvider,
                                   IErrorQueueService _errorQueueService,
                                   ILoggerFactory _loggerFactory)
{
    private const string route = "_admin/users/{assigneeUsername}/projects/{projectId:guid}/resource/{*resourceName}";

    private readonly IErrorQueueService _errorQueueService = _errorQueueService;
    private readonly ILogger<UserAssignmentHandler> _logger = _loggerFactory.CreateLogger<UserAssignmentHandler>();
    private readonly IProjectsService _projectsService = _projectsService;
    private readonly IUserProvider _userProvider = _userProvider;

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
            _logger.LogWarning($"Invalid assignment request: {exception.Message}");
            return req.BadRequest($"Invalid {exception.ParamName}.");
        }
        catch (ResourceNotFoundException exception)
        {
            _logger.LogWarning($"Invalid assignment request: {exception.Message}");
            return req.BadRequest("No resource could be found with the specified name.");
        }
        catch (UserAssignmentException exception)
        {
            _logger.LogWarning($"Invalid assignment request: {exception.Message}");
            return req.BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception during assignment request.");
            await _errorQueueService.ReportAsync(exception);
            return req.InternalServerError(exception.Message);
        }
    }

    private UserAssignment GetUserAssignment(string assigneeUsername, Guid projectId, string resourceName)
    {
        return new UserAssignment
        {
            AssignedByUsername = _userProvider.Username,
            AssignedOnUtc = DateTime.UtcNow,
            AssigneeUsername = assigneeUsername,
            ProjectId = projectId,
            ResourceFullName = $"{projectId}/{resourceName}"
        };
    }
}
