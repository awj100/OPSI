using System.Text.Json;
using Opsi.Abstractions;
using Opsi.Pocos;

namespace Opsi.Services;

internal class ManifestService : IManifestService
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

        if (!formFiles.TryGetValue(ManifestName, out var fileStream))
        {
            throw new Exception($"No {ManifestName} could be found.");
        }

        using (var content = new MemoryStream())
        {
            await fileStream.CopyToAsync(content);

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
