using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Opsi.AzureStorage.Types;
using Opsi.Pocos.History;
using Opsi.Services;
using Opsi.Services.QueueServices;
using ResourceVersion = Opsi.Pocos.History.ResourceVersion;

namespace Opsi.Functions.Functions;

public class ResourceHistoryHandler
{
    private const string route = "projects/{projectId:guid}/history/{*restOfPath}";

    private readonly IErrorQueueService _errorQueueService;
    private readonly ILogger<ResourceHandler> _logger;
    private readonly IResponseSerialiser _responseSerialiser;
    private readonly IResourceService _resourceService;

    public ResourceHistoryHandler(IResourceService resourceService,
                                  IErrorQueueService errorQueueService,
                                  IResponseSerialiser responseSerialiser,
                                  ILoggerFactory loggerFactory)
    {
        _errorQueueService = errorQueueService;
        _logger = loggerFactory.CreateLogger<ResourceHandler>();
        _responseSerialiser = responseSerialiser;
        _resourceService = resourceService;
    }

    [Function(nameof(ResourceHistoryHandler))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)] HttpRequestData req,
                                             Guid projectId,
                                             string? restOfPath = null)
    {
        _logger.LogInformation(nameof(ResourceHistoryHandler), projectId, restOfPath);

        var response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            if (String.IsNullOrEmpty(restOfPath))
            {
                var history = await _resourceService.GetResourcesHistoryAsync(projectId);
                var groupedHistoricVersions = ConvertGroupedVersionedResourceInfos(history);

                _responseSerialiser.WriteJsonToBody(response, groupedHistoricVersions);
            }
            else
            {
                var history = await _resourceService.GetResourceHistoryAsync(projectId, restOfPath);
                var historicVersions = ConvertVersionedResourceInfos(history);

                _responseSerialiser.WriteJsonToBody(response, historicVersions);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"{nameof(ResourceHistoryHandler)}: An error occurred while querying resource(s) history.", exception);

            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync(exception.Message);

            await _errorQueueService.ReportAsync(exception);
        }

        return response;
    }

    private static ResourceVersion ConvertVersionedResourceInfo(VersionedResourceStorageInfo versionedResourceStorage)
    {
        return new ResourceVersion(versionedResourceStorage.RestOfPath,
                                   versionedResourceStorage.Username,
                                   versionedResourceStorage.VersionId,
                                    versionedResourceStorage.VersionInfo.Index);
    }

    private static IReadOnlyCollection<ResourceVersion> ConvertVersionedResourceInfos(IEnumerable<VersionedResourceStorageInfo> versionedResourceStorages)
    {
        return versionedResourceStorages.Select(ConvertVersionedResourceInfo).ToList();
    }

    private static IReadOnlyCollection<GroupedResourceVersion> ConvertGroupedVersionedResourceInfos(IEnumerable<IGrouping<string, VersionedResourceStorageInfo>> groupedResourceStorageInfos)
    {
        return groupedResourceStorageInfos.Select(groupedVersions => {
            var versions = ConvertVersionedResourceInfos(groupedVersions);
            return new GroupedResourceVersion(groupedVersions.Key, versions);
        }).ToList();
    }
}
