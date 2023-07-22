using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Opsi.Functions.Specs;

public class TestFactory
{
    public static HttpRequestData CreateHttpRequest(string uri)
    {
        var body = new MemoryStream();
        var context = A.Fake<FunctionContext>();
        var request = new FakeHttpRequestData(context, new Uri(uri), body);

        return request;
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return NullLoggerFactory.Instance.CreateLogger<T>();
    }
}