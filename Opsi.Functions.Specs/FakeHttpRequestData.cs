using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions.Specs;

public class FakeHttpRequestData : HttpRequestData
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public FakeHttpRequestData(FunctionContext functionContext, Uri url, string method = "get", Stream? body = null) : base(functionContext)
    {
        Url = url;
        Body = body ?? new MemoryStream();
        Method = method;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public override Stream Body { get; } = new MemoryStream();

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();

    public override IEnumerable<ClaimsIdentity> Identities { get; }

    public override string Method { get; }

    public override Uri Url { get; }

    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
}

