using Opsi.Abstractions;

namespace Opsi.Functions2.FormHelpers;

public interface IMultipartFormDataParser
{
    Task<IFormFileCollection> ExtractFilesAsync(Stream httpRequestBody);
}