using System.Net;
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
    private CallbackMessage _callbackMessage;
    private IHttpClientFactory _httpClientFactory;
    private Uri _remoteUri;
    private WebhookDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _callbackMessage = new CallbackMessage
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

        var result = await _testee.AttemptDeliveryAsync(_callbackMessage, _remoteUri);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenRequestIsUnsuccessful_ReturnsFalse()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_remoteUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_callbackMessage, _remoteUri);

        result.Should().BeFalse();
    }
    
    [TestMethod]
    public async Task AttemptDeliveryAsync_DeliversSpecifiedCallbackMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalCallbackMessage = new InternalCallbackMessage(_callbackMessage, _remoteUriAsString);

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new ContentConditionalUriAndResponse<CallbackMessage>(_remoteUri,
                                                                  responseMessage,
                                                                  cm => cm.ProjectId.Equals(_callbackMessage.ProjectId)
                                                                        && cm.Status.Equals(_callbackMessage.Status),
                                                                  json => System.Text.Json.JsonSerializer.Deserialize<CallbackMessage>(json)!);
        
        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_callbackMessage, _remoteUri);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenPassedInternalCallbackMessage_DeliversCallbackMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalCallbackMessage = new InternalCallbackMessage(_callbackMessage, _remoteUriAsString);

        // Verify that the serialised content does not contain a property declared on InternalCallbackMessage.
        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new RequestConditionalUriAndResponse(_remoteUri,
                                                                  responseMessage,
                                                                  async req => {
                                                                      if (req.Content is not StringContent stringContent)
                                                                      {
                                                                          return false;
                                                                      }
                                                                      var serialisedContent = await stringContent.ReadAsStringAsync();
                                                                      return !serialisedContent.Contains(nameof(InternalCallbackMessage.FailureCount));
                                                                  });

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(internalCallbackMessage, _remoteUri);

        result.Should().BeTrue();
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(HttpClientNames.SelfWithoutAuth)).Returns(httpClient);
    }
}
