using Opsi.Abstractions;

namespace Opsi.Functions.FormHelpers;

public interface IMultipartFormDataParser
{
    Task<IFormFileCollection> ExtractFilesAsync(Stream httpRequestBody);
}