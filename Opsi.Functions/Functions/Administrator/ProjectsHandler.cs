using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common;
using Opsi.Constants;
using Opsi.Services;
using Opsi.Services.QueueServices;

namespace Opsi.Functions.Functions.Administrator;

public class ProjectsHandler(IProjectsService _projectsService,
                       IResponseSerialiser _responseSerialiser,
                       IErrorQueueService _errorQueueService,
                       ILoggerFactory loggerFactory)
{
    private const int defaultPageSize = 50;
    private const string route = "_admin/projects/{projectState}";

    private readonly string defaultOrderBy = OrderBy.Asc;
    private readonly ILogger<ProjectsHandler> _logger = loggerFactory.CreateLogger<ProjectsHandler>();

    [Function(nameof(ProjectsHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req,
                                            string projectState,
                                            string? orderBy = null,
                                            int? pageSize = null,
                                            string? continuationToken = null)
    {
        pageSize ??= defaultPageSize;

        _logger.LogInformation($"{nameof(ProjectsHandler)}: {nameof(projectState)} = \"{projectState}\".");

        var optOrderBy = orderBy != null ? OrderByExtensions.GetValidOrderBy(orderBy) : Option<string>.Some(defaultOrderBy);
        if (optOrderBy.IsNone)
        {
            return req.BadRequest($"Invalid orderby: \"{orderBy}\".");
        }

        try
        {
            var pageableProjectsResponse = await _projectsService.GetProjectsAsync(projectState, optOrderBy.Value, (int)pageSize, continuationToken);

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
