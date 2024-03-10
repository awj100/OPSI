using Opsi.Abstractions;

namespace Opsi.Functions.FormHelpers;

internal class MultipartFormDataParser : IMultipartFormDataParser
{
    public async Task<IFormFileCollection> ExtractFilesAsync(Stream httpRequestBody)
    {
        // https://github.com/Http-Multipart-Data-Parser/Http-Multipart-Data-Parser

        var filestreamsByName = new FormFileCollection();

        var parser = await HttpMultipartParser.MultipartFormDataParser.ParseAsync(httpRequestBody);

        foreach (var file in parser.Files)
        {
            filestreamsByName[file.FileName] = file.Data;
        }

        //var parser = new StreamingMultipartFormDataParser(httpRequestBody);
        ////parser.ParameterHandler += parameter => DoSomethingWithParameter(parameter);
        //parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, partNumber, additionalProperties) => filestreamsByName[name].Write(buffer, 0, bytes);

        //await parser.RunAsync().ConfigureAwait(false);

        return filestreamsByName;
    }
}
