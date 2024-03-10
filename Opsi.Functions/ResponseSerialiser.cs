using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions;

internal class ResponseSerialiser : IResponseSerialiser
{
    public void WriteJsonToBody<T>(HttpResponseData response, T instance)
    {
        var serialisationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var json = JsonSerializer.Serialize(instance, serialisationOptions);
        var stringBytes = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream(stringBytes);

        response.Body = memoryStream;
        response.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        response.StatusCode = HttpStatusCode.OK;
    }
}
