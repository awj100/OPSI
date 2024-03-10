using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions.Specs;

public class FakeHttpResponseData : HttpResponseData
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies { get; }

    public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();

    public override HttpStatusCode StatusCode { get; set; }
}
