using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Opsi.Services.QueueHandlers.Dependencies;

namespace Opsi.Services.Specs.QueueHandlers;

[TestClass]
public class UnzipServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReadOnlyCollection<string> _archiveContentFilePaths;
    private Stream _archiveStream;
    private UnzipService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _archiveContentFilePaths = new List<string>
        {
            GetTestFilePath(0),
            GetTestFilePath(1)
        };
        _archiveStream = GetArchiveStream(_archiveContentFilePaths);

        _testee = new UnzipService(_archiveStream);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _archiveStream?.Dispose();
        _testee.Dispose();
    }

    [TestMethod]
    public async Task GetContentsAsync_ReturnsExpectedContents()
    {
        var filePath = _archiveContentFilePaths.First();
        var expectedContents = GetContentsForTestFile(filePath);

        using (var stream = await _testee.GetContentsAsync(filePath))
        {
            stream.Should().NotBeNull();

            var contents = ReadStreamContentsAsText(stream!);

            contents.Should().Be(expectedContents);
        }
    }

    [TestMethod]
    public void GetFilePathsFromPackage_ReturnsExpectedPaths()
    {
        var result = _testee.GetFilePathsFromPackage();

        result.Should()
              .Contain(_archiveContentFilePaths).And
              .HaveCount(_archiveContentFilePaths.Count());
    }

    private static Stream GetArchiveStream(IReadOnlyCollection<string> filePaths)
    {
        static void AddFile(ZipArchive archive, string filePath)
        {
            ZipArchiveEntry entry = archive.CreateEntry(filePath);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(GetContentsForTestFile(filePath));
        }

        var stream = new MemoryStream();

        using (var writableStream = new MemoryStream())
        {
            using (ZipArchive archive = new(writableStream, ZipArchiveMode.Create, true))
            {
                foreach (var filePath in filePaths)
                {
                    AddFile(archive, filePath);
                }
            }

            writableStream.Seek(0, SeekOrigin.Begin);
            writableStream.CopyTo(stream);
        }

        stream.Position = 0;

        return stream;
    }

    private static string GetContentsForTestFile(string filePath)
    {
        return $"{filePath} contents";
    }

    private static string GetTestFilePath(int fileIndex)
    {
        return $"testfile_{fileIndex}.txt";
    }

    private static string ReadStreamContentsAsText(Stream stream)
    {
        return String.Join(Environment.NewLine, ReadStreamLinesAsText(stream).ToList());
    }

    private static IEnumerable<string> ReadStreamLinesAsText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }
}
