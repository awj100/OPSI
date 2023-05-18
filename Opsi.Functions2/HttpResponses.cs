using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions2;

public static class HttpResponses
{
    private const string HttpHeaderContentType = "Content-Type";
    private const string Utf8TextPlain = "text/plain; charset=utf-8";

    public static HttpResponseData Accepted(this HttpRequestData req)
    {
        return ResponseWithoutBody(req, HttpStatusCode.Accepted);
    }

    public static HttpResponseData BadRequest(this HttpRequestData req, string message)
    {
        return ResponseWithPlainText(req, HttpStatusCode.BadRequest, message);
    }

    public static HttpResponseData InternalServerError(this HttpRequestData req, string message)
    {
        return ResponseWithPlainText(req, HttpStatusCode.InternalServerError, message);
    }

    public static HttpResponseData Ok(this HttpRequestData req)
    {
        return ResponseWithoutBody(req, HttpStatusCode.OK);
    }

    private static HttpResponseData ResponseWithoutBody(this HttpRequestData req, HttpStatusCode httpStatusCode)
    {
        return req.CreateResponse(httpStatusCode);
    }

    private static HttpResponseData ResponseWithPlainText(this HttpRequestData req, HttpStatusCode httpStatusCode, string message)
    {
        var response = req.CreateResponse(httpStatusCode);

        response.Headers.Add(HttpHeaderContentType, Utf8TextPlain);

        response.WriteString(message);

        return response;
    }
}
