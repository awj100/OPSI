﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.Common.Exceptions;
using Opsi.Functions2.FormHelpers;
using Opsi.Services;

namespace Opsi.Functions2;

public class ProjectUploadHandler
{
    private readonly IErrorQueueService _errorService;
    private readonly ILogger<ProjectUploadHandler> _logger;
    private readonly IMultipartFormDataParser _multipartFormDataParser;
    private readonly IProjectUploadService _projectUploadService;

    public ProjectUploadHandler(IMultipartFormDataParser multipartFormDataParser,
                                IProjectUploadService projectUploadService,
                                IErrorQueueService errorQueueService,
                                ILoggerFactory loggerFactory)
    {
        _errorService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ProjectUploadHandler>();
        _multipartFormDataParser = multipartFormDataParser;
        _projectUploadService = projectUploadService;
    }

    [Function(nameof(ProjectUploadHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "projects/upload")] HttpRequestData req)
    {
        _logger.LogInformation(nameof(ProjectUploadHandler));

        Abstractions.IFormFileCollection files;
        try
        {
            files = await _multipartFormDataParser.ExtractFilesAsync(req.Body);
        }
        catch (Exception exception)
        {
            await _errorService.ReportAsync(exception);
            return req.BadRequest("Unable to extract the expected files from the upload.");
        }

        try
        {
            await _projectUploadService.StoreInitialProjectUploadAsync(files);
        }
        catch (BadRequestException exception)
        {
            await _errorService.ReportAsync(exception);
            return req.BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            await _errorService.ReportAsync(exception);
            return req.InternalServerError("An exception was thrown while storing the upload.");
        }

        return req.Accepted();
    }
}

