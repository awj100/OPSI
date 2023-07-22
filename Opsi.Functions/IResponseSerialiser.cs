using Microsoft.Azure.Functions.Worker.Http;

namespace Opsi.Functions;

public interface IResponseSerialiser
{
    void WriteJsonToBody<T>(HttpResponseData response, T instance);
}