﻿using System.Net;
using FakeItEasy;
using FluentAssertions;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.InternalTypes;
using Opsi.Services.Specs.Http;
using Opsi.Services.Webhooks;

namespace Opsi.Services.Specs.Webhooks;

[TestClass]
public class WebhookDispatcherSpecs
{
    private const string _remoteUriAsString = "https://test.url.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private WebhookMessage _webhookMessage;
    private IHttpClientFactory _httpClientFactory;
    private Uri _remoteUri;
    private WebhookDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _webhookMessage = new WebhookMessage
        {
            ProjectId = Guid.NewGuid(),
            Status = Guid.NewGuid().ToString()
        };
        _remoteUri = new(_remoteUriAsString);

        _httpClientFactory = A.Fake<IHttpClientFactory>();

        _testee = new WebhookDispatcher(_httpClientFactory);
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenRequestIsSuccessful_ReturnsTrue()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_remoteUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenRequestIsUnsuccessful_ReturnsFalse()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_remoteUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri);

        result.Should().BeFalse();
    }
    
    [TestMethod]
    public async Task AttemptDeliveryAsync_DeliversSpecifiedWebhookMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _remoteUriAsString);

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new ContentConditionalUriAndResponse<WebhookMessage>(_remoteUri,
                                                                                  responseMessage,
                                                                                  cm => cm.ProjectId.Equals(_webhookMessage.ProjectId)
                                                                                        && cm.Status.Equals(_webhookMessage.Status),
                                                                                  json => System.Text.Json.JsonSerializer.Deserialize<WebhookMessage>(json)!);
        
        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenPassedInternalWebhookMessage_DeliversWebhookMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _remoteUriAsString);

        // Verify that the serialised content does not contain a property declared on InternalWebhookMessage.
        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new RequestConditionalUriAndResponse(_remoteUri,
                                                                  responseMessage,
                                                                  async req => {
                                                                      if (req.Content is not StringContent stringContent)
                                                                      {
                                                                          return false;
                                                                      }
                                                                      var serialisedContent = await stringContent.ReadAsStringAsync();
                                                                      return !serialisedContent.Contains(nameof(InternalWebhookMessage.FailureCount));
                                                                  });

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(internalWebhookMessage, _remoteUri);

        result.Should().BeTrue();
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(HttpClientNames.SelfWithoutAuth)).Returns(httpClient);
    }
}
