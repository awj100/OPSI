using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Opsi.Pocos;

namespace Opsi.Services;

public class ManifestService : IManifestService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ManifestName => "manifest.json";

    public async Task<Manifest> GetManifestAsync(IFormFileCollection formFiles)
    {
        var jsonString = String.Empty;

        IFormFile? manifestFile = null;
        foreach (var formFile in formFiles)
        {
            if (!String.Equals(formFile.FileName, ManifestName, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            manifestFile = formFile;
            break;
        }

        if (manifestFile == null)
        {
            throw new Exception($"No {ManifestName} could be found.");
        }

        using (var content = new MemoryStream())
        {
            await manifestFile.CopyToAsync(content);

            content.Position = 0;

            jsonString = System.Text.Encoding.UTF8.GetString(content.ToArray());
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(jsonString, _jsonOptions);
            return manifest ?? throw new Exception($"{ManifestName} could not be deserialised.");
        }
        catch (Exception)
        {
            throw new Exception($"{ManifestName} could not be deserialised.");
        }
    }
}
