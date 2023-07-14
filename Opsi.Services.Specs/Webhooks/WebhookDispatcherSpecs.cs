using System.Net;
using System.Text.Json;
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
    private const string _customProp1Name = nameof(_customProp1Name);
    private const string _customProp1Value = nameof(_customProp1Value);
    private const string _customProp2Name = nameof(_customProp2Name);
    private const int _customProp2Value = 2;
    private const string _event = "TEST EVENT";
    private const string _level = "TEST LEVEL";
    private const string _name = "TEST NAME";
    private const string _remoteUriAsString = "https://test.url.com";
    private const string _username = "user@test.com";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Guid _id;
    private Dictionary<string, object> _webhookCustomProps;
    private WebhookMessage _webhookMessage;
    private ConsumerWebhookSpecification _webhookSpec;
    private IHttpClientFactory _httpClientFactory;
    private JsonSerializerOptions _jsonSerialiserOptions;
    private Uri _remoteUri;
    private IUserProvider _userProvider;
    private WebhookDispatcher _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _id = Guid.NewGuid();
        _jsonSerialiserOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        _webhookCustomProps = new Dictionary<string, object>
        {
            { _customProp1Name, _customProp1Value },
            { _customProp2Name, _customProp2Value }
        };

        _webhookMessage = new WebhookMessage
        {
            Event = _event,
            Id = _id,
            Level = _level,
            Name = _name,
            ProjectId = Guid.NewGuid(),
            Username = _username
        };

        _webhookSpec = new ConsumerWebhookSpecification
        {
            CustomProps = _webhookCustomProps,
            Uri = _remoteUriAsString
        };

        _remoteUri = new(_remoteUriAsString);

        _httpClientFactory = A.Fake<IHttpClientFactory>();
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _username));
        
        _testee = new WebhookDispatcher(_httpClientFactory);
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenRequestIsSuccessful_ReturnsTrue()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_remoteUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri, _webhookCustomProps);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenRequestIsUnsuccessful_ReturnsFalse()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new UriAndResponse(_remoteUri, responseMessage);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri, _webhookCustomProps);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }
    
    [TestMethod]
    public async Task AttemptDeliveryAsync_DeliversDispatchableWebhookMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _webhookSpec);

        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var uriAndResponse = new ContentConditionalUriAndResponse<DispatchableWebhookMessage>(_remoteUri,
                                                                                              responseMessage,
                                                                                              dwm => dwm.Id.Equals(_id)
                                                                                                     && dwm.CustomProps != null
                                                                                                     && dwm.CustomProps.Count.Equals(_webhookCustomProps.Count),
                                                                                              json => JsonSerializer.Deserialize<DispatchableWebhookMessage>(json, _jsonSerialiserOptions)!);

        ConfigureHttpResponse(uriAndResponse);

        var result = await _testee.AttemptDeliveryAsync(_webhookMessage, _remoteUri, _webhookCustomProps);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task AttemptDeliveryAsync_WhenPassedInternalWebhookMessage_DeliversWebhookMessage()
    {
        const HttpStatusCode httpStatusCode = HttpStatusCode.OK;

        var internalWebhookMessage = new InternalWebhookMessage(_webhookMessage, _webhookSpec);

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

        var result = await _testee.AttemptDeliveryAsync(internalWebhookMessage, _remoteUri, _webhookCustomProps);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    private void ConfigureHttpResponse(UriAndResponse uriAndResponse)
    {
        var testMessageHandler = new TestHttpMessageHandler(uriAndResponse);
        var httpClient = new HttpClient(testMessageHandler);

        A.CallTo(() => _httpClientFactory.CreateClient(HttpClientNames.SelfWithoutAuth)).Returns(httpClient);
    }
}
