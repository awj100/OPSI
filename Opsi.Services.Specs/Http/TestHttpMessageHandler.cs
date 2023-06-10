using System.Net;

namespace Opsi.Services.Specs.Http;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly IList<UriAndResponse> _urisAndResponses;

    public TestHttpMessageHandler(UriAndResponse uriAndResponse)
    {
        _urisAndResponses = new[] { uriAndResponse };
    }

    public TestHttpMessageHandler(IEnumerable<UriAndResponse> urisAndResponses)
    {
        _urisAndResponses = urisAndResponses.ToList();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // For each matching URI check whether there is a non-NotImplented response.
        foreach (var uriAndResponse in _urisAndResponses.Where(uri => request.RequestUri != null && uri.Uri.AbsoluteUri == request.RequestUri.AbsoluteUri))
        {
            var response = await uriAndResponse.HandleRequest(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.NotImplemented)
            {
                return response;
            }
        }

        // If there was no non-NotImplemented response then return a NotImplemented response.
        return new HttpResponseMessage(HttpStatusCode.NotImplemented);
    }
}