using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.Webhooks;

internal class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookDispatcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> AttemptDeliveryAsync(CallbackMessage callbackMessage, Uri remoteUri)
    {
        if (callbackMessage is InternalCallbackMessage message)
        {
            callbackMessage = message.ToCallbackMessage();
        }

        var serialisedContent = JsonSerializer.Serialize(callbackMessage);

        using (var httpClient = _httpClientFactory.CreateClient(HttpClientNames.SelfWithoutAuth))
        using (var content = new MultipartFormDataContent())
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            using (var stringContent = new StringContent(serialisedContent))
            {
                var response = await httpClient.PostAsync(remoteUri, stringContent);

                return response.IsSuccessStatusCode;
            }
        }
    }
}
