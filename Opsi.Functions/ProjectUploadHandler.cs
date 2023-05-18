using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Services;

namespace Opsi.Functions;

public class ProjectUploadHandler
{
    private readonly IErrorQueueService _errorQueueService;
    private readonly IProjectUploadService _projectUploadService;

    public ProjectUploadHandler(IProjectUploadService projectUploadService, IErrorQueueService errorQueueService)
    {
        _errorQueueService = errorQueueService;
        _projectUploadService = projectUploadService;
    }

    [FunctionName(nameof(ProjectUploadHandler))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/upload")] HttpRequest req, ILogger log)
    {
        log.LogInformation(nameof(ProjectUploadHandler));

        var files = req.Form.Files;

        try
        {
            await _projectUploadService.StoreInitialProjectUploadAsync(files, log);
        }
        catch(BadRequestException exception)
        {
            await _errorQueueService.ReportAsync(exception);

            return new BadRequestObjectResult(exception.Message);
        }
        catch (Exception exception)
        {
            await _errorQueueService.ReportAsync(exception);

            throw new Exception(exception.Message);
        }

        return new AcceptedResult();
    }
}
