using System.IO.Compression;
using Opsi.Common;

namespace Opsi.TradosStudio;

internal class PackageService : IPackageService
{
    private ZipArchive? _archive;
    private readonly Stream _packageStream;

    public PackageService(Stream packageStream)
    {
        _packageStream = packageStream;
    }

    public IReadOnlyCollection<string> GetFilePathsFromPackage()
    {
        const string macOsPrefix = "__MACOS";
        const string macOsSuffix = ".DS_Store";

        if (_archive == null)
        {
            _archive = new ZipArchive(_packageStream, ZipArchiveMode.Read);
        }

        return _archive.Entries
            .Where(entry => !entry.FullName.StartsWith(macOsPrefix)
            && !entry.FullName.EndsWith(macOsSuffix)
            && !entry.FullName.EndsWith("/"))
            .Select(entry => entry.FullName).ToList();
    }

    public async Task<Stream?> GetContentsAsync(string fullName)
    {
        if (_archive == null)
        {
            _archive = new ZipArchive(_packageStream, ZipArchiveMode.Read);
        }

        var entryStream = new MemoryStream();

        ZipArchiveEntry entry = _archive.GetEntry(fullName);
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
}
