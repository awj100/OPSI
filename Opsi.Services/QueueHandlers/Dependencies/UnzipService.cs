using System.IO.Compression;

namespace Opsi.Services.QueueHandlers.Dependencies;

internal class UnzipService : IUnzipService
{
    private ZipArchive? _archive;
    private readonly Stream _packageStream;

    public UnzipService(Stream packageStream)
    {
        _packageStream = packageStream;
    }

    public IReadOnlyCollection<string> GetFilePathsFromPackage()
    {
        const string macOsPrefix = "__MACOS";
        const string macOsSuffix = ".DS_Store";

        var archive = GetArchive();

        return archive.Entries
            .Where(entry => !entry.FullName.StartsWith(macOsPrefix)
                            && !entry.FullName.EndsWith(macOsSuffix)
                            && !entry.FullName.EndsWith("/"))
            .Select(entry => entry.FullName).ToList();
    }

    public async Task<Stream?> GetContentsAsync(string fullName)
    {
        var archive = GetArchive();

        var entryStream = new MemoryStream();

        ZipArchiveEntry? entry = archive.GetEntry(fullName);
        if (entry == null)
        {
            return null;
        }

        using (var stream = entry.Open())
        {
            await stream.CopyToAsync(entryStream);
        }

        entryStream.Position = 0;

        return entryStream;
    }

    public void Dispose()
    {
        _packageStream?.Dispose();
        _archive?.Dispose();
    }

    private ZipArchive GetArchive()
    {
        if (_archive != null)
        {
            return _archive;
        }

        try
        {
            _archive = new ZipArchive(_packageStream, ZipArchiveMode.Read);
        }
        catch (Exception exception)
        {
            throw new Exception($"Unable to create {nameof(ZipArchive)} from specified stream.", exception);
        }

        return _archive;
    }
}
