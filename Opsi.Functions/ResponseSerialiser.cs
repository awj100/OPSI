using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions;

internal class ResponseSerialiser : IResponseSerialiser
{
    private static readonly JsonSerializerOptions SerialiserOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void WriteJsonToBody<T>(HttpResponseData response, T instance)
    {
        var json = JsonSerializer.Serialize(instance, SerialiserOptions);
        var stringBytes = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream(stringBytes);

        response.Body = memoryStream;
        response.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        response.StatusCode = HttpStatusCode.OK;
    }
}
