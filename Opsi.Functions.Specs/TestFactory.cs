using System.Text;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Opsi.Functions.Specs;

public class TestFactory
{
    private const string MethodGet = "get";

    public static HttpRequestData CreateHttpRequest(string uri, string method = MethodGet)
    {
        var body = new MemoryStream();
        var context = A.Fake<FunctionContext>();
        return new FakeHttpRequestData(context, new Uri(uri), method, body);
    }

    public static async Task<HttpRequestData> CreateHttpRequestAsync(string uri, string method, object objectToPostOrPut)
    {
        var serialisedContent = JsonConvert.SerializeObject(objectToPostOrPut);
        
        var body = new MemoryStream();
        await body.WriteAsync(Encoding.UTF8.GetBytes(serialisedContent)
                                           .AsMemory(0, serialisedContent.Length));
        body.Seek(0, SeekOrigin.Begin);

        var context = A.Fake<FunctionContext>();
        var request = new FakeHttpRequestData(context, new Uri(uri), method, body);

        return request;
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return NullLoggerFactory.Instance.CreateLogger<T>();
    }
}