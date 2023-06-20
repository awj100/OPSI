using System.Net.Http.Headers;
using Opsi.Services.Auth.OneTimeAuth;

namespace Opsi.Services.QueueHandlers.Dependencies;

internal class ResourceDispatcher : IResourceDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOneTimeAuthService _oneTimeAuthService;

    public ResourceDispatcher(IHttpClientFactory httpClientFactory, IOneTimeAuthService oneTimeAuthService)
    {
        _httpClientFactory = httpClientFactory;
        _oneTimeAuthService = oneTimeAuthService;
    }

    public async Task<HttpResponseMessage> DispatchAsync(string hostUrl,
                                                         Guid projectId,
                                                         string filePath,
                                                         Stream contentsStream,
                                                         string username)
    {
        const string mediaTypeHeaderValue = "application/octet-stream";

        var fileName = Path.GetFileName(filePath);

        using (var httpClient = _httpClientFactory.CreateClient(Constants.HttpClientNames.SelfWithoutAuth))
        using (var content = new MultipartFormDataContent())
        {
            httpClient.DefaultRequestHeaders.Authorization = await _oneTimeAuthService.GetAuthenticationHeaderAsync(username, projectId, filePath);

            content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            using (var streamContent = new StreamContent(contentsStream))
            {
                content.Add(streamContent, fileName);

                var url = new Uri($"{hostUrl}/projects/{projectId}/resource/{filePath}");
                return await httpClient.PostAsync(url, content);
            }
        }
    }
}
