using System.Net.Http.Headers;
using Opsi.Constants;
using Opsi.Services.Auth.OneTimeAuth;

namespace Opsi.Services.QueueHandlers.Dependencies;

internal class ResourceDispatcher(IHttpClientFactory _httpClientFactory, IOneTimeAuthService _oneTimeAuthService) : IResourceDispatcher
{
    public async Task<HttpResponseMessage> DispatchAsync(string hostUrl,
                                                         Guid projectId,
                                                         string filePath,
                                                         Stream contentsStream,
                                                         string username,
                                                         bool isAdministrator)
    {
        const string mediaTypeHeaderValue = "application/octet-stream";

        var fileName = Path.GetFileName(filePath);

        using (var httpClient = _httpClientFactory.CreateClient(HttpClientNames.OneTimeAuth))
        using (var content = new MultipartFormDataContent())
        {
            httpClient.DefaultRequestHeaders.Authorization = await _oneTimeAuthService.GetAuthenticationHeaderAsync(username, isAdministrator);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            using (var streamContent = new StreamContent(contentsStream))
            {
                content.Add(streamContent, fileName);

                var url = new Uri($"{hostUrl}/projects/{projectId}/resources/{filePath}");

                return await httpClient.PostAsync(url, content);
            }
        }
    }
}
