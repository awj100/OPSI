using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;

namespace Opsi.Services.Webhooks;

internal class WebhookDispatcher : IWebhookDispatcher
{
    private readonly JsonSerializerOptions _jsonSerialiserOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookDispatcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WebhookDispatchResponse> AttemptDeliveryAsync(WebhookMessage webhookMessage, Uri remoteUri, Dictionary<string, object> customProps)
    {
        if (webhookMessage is InternalWebhookMessage message)
        {
            webhookMessage = message.ToWebhookMessage();
        }

        var dispatchableWebhookMessage = DispatchableWebhookMessage.FromWebhookMessage(webhookMessage, customProps);

        var serialisedContent = JsonSerializer.Serialize(dispatchableWebhookMessage, _jsonSerialiserOptions);

        using (var httpClient = _httpClientFactory.CreateClient(HttpClientNames.SelfWithoutAuth))
        using (var content = new MultipartFormDataContent())
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            using (var stringContent = new StringContent(serialisedContent))
            {
                var dispatchResponse = new WebhookDispatchResponse();

                try
                {
                    dispatchResponse.IsSuccessful = (await httpClient.PostAsync(remoteUri, stringContent)).IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    dispatchResponse.FailureReason = ex.Message;
                }

                return dispatchResponse;
            }
        }
    }
}
