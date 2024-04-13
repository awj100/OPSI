using System.Text.Json;
using FluentAssertions;
using Opsi.Abstractions;
using Opsi.Functions.FormHelpers;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ManifestServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private const string HandlerQueueName = "test handler queue";
    private IFormFileCollection _formFileCollection;
    private Manifest _manifest;
    private Stream _manifestStream;
    private Stream _nonManifestStream;
    private ManifestService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _manifest = new Manifest { HandlerQueue = HandlerQueueName };
        var manifestAsJson = JsonSerializer.Serialize(_manifest);
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes(manifestAsJson);
        _manifestStream = new MemoryStream(manifestBytes);
        _nonManifestStream = new MemoryStream();
        _formFileCollection = new FormFileCollection
        {
            { ManifestService.IncomingManifestName, _manifestStream },
            { "non_manifest_object", _nonManifestStream }
        };

        _testee = new ManifestService();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _manifestStream?.Dispose();
        _nonManifestStream?.Dispose();
    }

    [TestMethod]
    public async Task GetManifestAsync_WhenManifestIsPresent_ReturnsExpectedManifest()
    {
        var retrievedManifest = await _testee.ExtractManifestAsync(_formFileCollection);

        retrievedManifest.Should()
            .NotBeNull()
            .And.Match<Manifest>(m => m.HandlerQueue.Equals(HandlerQueueName));
    }

    [TestMethod]
    public async Task GetManifestAsync_WhenManifestNotPresent_ThrowsMeaningfulException()
    {
        _formFileCollection.Remove(_formFileCollection.Single(ff => ff.Key == ManifestService.IncomingManifestName));

        await _testee.Invoking(t => t.ExtractManifestAsync(_formFileCollection))
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage($"*{ManifestService.IncomingManifestName}*");
    }
}
