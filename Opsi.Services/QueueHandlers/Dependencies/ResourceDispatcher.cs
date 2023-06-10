using System.Net.Http.Headers;

namespace Opsi.Services.QueueHandlers.Dependencies;

internal class ResourceDispatcher : IResourceDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ResourceDispatcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HttpResponseMessage> DispatchAsync(string hostUrl, Guid projectId, string filePath, Stream contentsStream)
    {
        const string mediaTypeHeaderValue = "application/octet-stream";

        var fileName = Path.GetFileName(filePath);

        using (var httpClient = _httpClientFactory.CreateClient())
        using (var content = new MultipartFormDataContent())
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            using (StreamContent streamContent = new StreamContent(contentsStream))
            {
                content.Add(streamContent, fileName);

                var url = new Uri($"{hostUrl}/projects/{projectId}/resource/{filePath}");
                return await httpClient.PostAsync(url, content);
            }
        }
    }
}
