using System.Text.Json;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Pocos;

namespace Opsi.Services;

internal class ManifestService(IBlobService _blobService, ITagUtilities _tagUtilities) : IManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string IncomingManifestName => "manifest.json";

    public async Task<Manifest> ExtractManifestAsync(IFormFileCollection formFiles)
    {
        var jsonString = String.Empty;

        if (!formFiles.TryGetValue(IncomingManifestName, out var fileStream))
        {
            throw new Exception($"No {IncomingManifestName} could be found.");
        }

        using (var content = new MemoryStream())
        {
            await fileStream.CopyToAsync(content);

            content.Position = 0;

            jsonString = System.Text.Encoding.UTF8.GetString(content.ToArray());
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(jsonString, JsonOptions);
            return manifest ?? throw new Exception($"{IncomingManifestName} could not be deserialised.");
        }
        catch (Exception)
        {
            throw new Exception($"{IncomingManifestName} could not be deserialised.");
        }
    }

    public string GetManifestFullName(Guid projectId)
    {
        return $"{projectId}/{Tags.ManifestName}";
    }

    public async Task<InternalManifest?> RetrieveManifestAsync(Guid projectId)
    {
        var blobName = GetManifestFullName(projectId);

        var blobContentStream = await _blobService.RetrieveContentAsync(blobName);
        if (blobContentStream == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<InternalManifest>(blobContentStream) ?? throw new ManifestFormatException();
    }

    public async Task<string> StoreManifestAsync(InternalManifest internalManifest)
    {
        var blobName = GetManifestFullName(internalManifest.ProjectId);
        var initialState = ProjectStates.Initialising;
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, internalManifest);
        stream.Seek(0, SeekOrigin.Begin);

        try
        {
            await _blobService.StoreResourceAsync(blobName, stream);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to create project with ID \"{internalManifest.ProjectId}\": {exception.Message}");
        }

        var metadata = new Dictionary<string, string>
        {
            {Metadata.CreatedBy, internalManifest.Username},
            {Metadata.ProjectId, internalManifest.ProjectId.ToString()},
            {Metadata.ProjectName, internalManifest.PackageName}
        };

        try
        {
            await _blobService.SetMetadataAsync(blobName, metadata);
        }
        catch (Exception exception)
        {
            try
            {
                await _blobService.DeleteAsync(blobName);
            }
            catch (Exception)
            { }

            throw new Exception($"Failed to set initial metadata on project with ID \"{internalManifest.ProjectId}\": {exception.Message}");
        }

        var tags = new Dictionary<string, string>
        {
            {Tags.Id, _tagUtilities.GetSafeTagValue(internalManifest.ProjectId)},
            {Tags.Name, _tagUtilities.GetSafeTagValue(internalManifest.PackageName)},
            {Tags.State, _tagUtilities.GetSafeTagValue(initialState)}
        };

        try
        {
            await _blobService.SetTagsAsync(blobName, tags);
        }
        catch (Exception exception)
        {
            try
            {
                await _blobService.DeleteAsync(blobName);
            }
            catch (Exception)
            { }

            throw new Exception($"Failed to set initial tags on project with ID \"{internalManifest.ProjectId}\": {exception.Message}");
        }

        return blobName;
    }
}
