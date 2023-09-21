using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opsi.Common;
using Opsi.Pocos;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class UserAssignmentHandler
{
    private const string route = "assignment";

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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = route)] HttpRequestData req)
    {
        _logger.LogInformation(nameof(UserAssignmentHandler));

        try
        {
            var optUserAssignment = await GetUserAssignmentAsync(req);
            if (optUserAssignment.IsNone)
            {
                return req.BadRequest($"Could not parse a valid UserAssignment from the request body.");
            }

            await _projectsService.AssignUserAsync(optUserAssignment.Value);

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

    private async Task<Option<UserAssignment>> GetUserAssignmentAsync(HttpRequestData request)
    {
        string requestBody = String.Empty;
        using (StreamReader streamReader = new(request.Body))
        requestBody = await streamReader.ReadToEndAsync();
        var userAssignment = JsonConvert.DeserializeObject<UserAssignment>(requestBody);

        if (userAssignment == null)
        {
            return Option<UserAssignment>.None();
        }

        userAssignment.AssignedByUsername = _userProvider.Username.Value;
        userAssignment.AssignedOnUtc = DateTime.UtcNow;

        return Option<UserAssignment>.Some(userAssignment);
    }
}

