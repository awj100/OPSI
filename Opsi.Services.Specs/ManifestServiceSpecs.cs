using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using Opsi.Abstractions;
using Opsi.AzureStorage;
using Opsi.Constants;
using Opsi.Functions.FormHelpers;
using Opsi.Pocos;

namespace Opsi.Services.Specs;

[TestClass]
public class ManifestServiceSpecs
{
    private const string HandlerQueueName = "test handler queue";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IBlobService _blobService;
    private IFormFileCollection _formFileCollection;
    private Manifest _manifest;
    private Stream _manifestStream;
    private Stream _nonManifestStream;
    private Guid _projectId;
    private ITagUtilities _tagUtilities;
    private ManifestService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _blobService = A.Fake<IBlobService>();
        _projectId = Guid.NewGuid();

        _manifest = new Manifest
        {
            HandlerQueue = HandlerQueueName,
            PackageName = "TEST NAME",
            ProjectId = _projectId
        };
        _manifestStream = new MemoryStream();
        JsonSerializer.Serialize(_manifestStream, _manifest);
        _manifestStream.Seek(0, SeekOrigin.Begin);
        _nonManifestStream = new MemoryStream();
        _formFileCollection = new FormFileCollection
        {
            { ManifestService.IncomingManifestName, _manifestStream },
            { "non_manifest_object", _nonManifestStream }
        };

        _tagUtilities = A.Fake<ITagUtilities>();
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        A.CallTo(() => _tagUtilities.GetSafeTagValue(A<object?>._)).ReturnsLazily((object? o) =>
        {
            if (o == null)
            {
                return null;
            }

            if (o is Guid g)
            {
                return g.ToString();
            }

            if (o is string s)
            {
                return s;
            }

            throw new Exception($"Unconfigured type in set-up of ITagUtilities.GetSafeTagValue: {o.GetType()}.");
        });
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        _testee = new ManifestService(_blobService, _tagUtilities);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _manifestStream?.Dispose();
        _nonManifestStream?.Dispose();
    }

    [TestMethod]
    public async Task ExtractManifestAsync_WhenManifestIsPresent_ReturnsExpectedManifest()
    {
        var retrievedManifest = await _testee.ExtractManifestAsync(_formFileCollection);

        retrievedManifest.Should()
            .NotBeNull()
            .And.Match<Manifest>(m => m.HandlerQueue.Equals(HandlerQueueName));
    }

    [TestMethod]
    public async Task ExtractManifestAsync_WhenManifestNotPresent_ThrowsMeaningfulException()
    {
        _formFileCollection.Remove(_formFileCollection.Single(ff => ff.Key == ManifestService.IncomingManifestName));

        await _testee.Invoking(t => t.ExtractManifestAsync(_formFileCollection))
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage($"*{ManifestService.IncomingManifestName}*");
    }

    [TestMethod]
    public void GetManifestFullName_IncludesProjectIdInPath()
    {
        var projectId = Guid.NewGuid();

        var fullName = _testee.GetManifestFullName(projectId);

        fullName.Should().Contain(projectId.ToString());
    }

    [TestMethod]
    public async Task RetrieveManifestAsync_WhenManifestExists_ReturnsManifest()
    {
        A.CallTo(() => _blobService.RetrieveContentAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())))).Returns(_manifestStream);

        var result = await _testee.RetrieveManifestAsync(_projectId);

        result.Should().NotBeNull();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        result.ProjectId.Should().Be(_projectId);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    [TestMethod]
    public async Task RetrieveManifestAsync_WhenNoManifestExists_ReturnsNull()
    {
        A.CallTo(() => _blobService.RetrieveContentAsync(A<string>._)).Returns((Stream?)null);

        var result = await _testee.RetrieveManifestAsync(_projectId);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStored_SetsCreatedByMetadata()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._,
                                                     A<Dictionary<string, string>>.That.Matches(dict => dict[Metadata.CreatedBy] == username)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStored_SetsProjectIdMetadata()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._,
                                                     A<Dictionary<string, string>>.That.Matches(dict => dict[Metadata.ProjectId] == _manifest.ProjectId.ToString())))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStored_SetsProjectNameMetadata()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._,
                                                     A<Dictionary<string, string>>.That.Matches(dict => dict[Metadata.ProjectName] == _manifest.PackageName)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsNotStored_MakesNoAttemptToStoreMetadata()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        A.CallTo(() => _blobService.StoreResourceAsync(A<string>._, A<Stream>._)).Throws<Exception>();

        try
        {
            await _testee.StoreManifestAsync(internalManifest);
        }
        catch (Exception)
        { }

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenMetadataIsNotSet_MakesNoAttemptToStoreTags()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).Throws<Exception>();

        try
        {
            await _testee.StoreManifestAsync(internalManifest);
        }
        catch (Exception)
        { }

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenMetadataIsNotSet_DeletesManifestBlob()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).Throws<Exception>();

        try
        {
            await _testee.StoreManifestAsync(internalManifest);
        }
        catch (Exception)
        { }

        A.CallTo(() => _blobService.DeleteAsync(A<string>.That.Matches(s => s.Contains(_manifest.ProjectId.ToString())))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStoredAndMetadataIsSet_SetsProjectIdTag()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._,
                                                 A<Dictionary<string, string>>.That.Matches(dict => dict[Tags.ProjectId] == _manifest.ProjectId.ToString())))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStoredAndMetadataIsSet_SetsProjectNameTag()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._,
                                                 A<Dictionary<string, string>>.That.Matches(dict => dict[Tags.ProjectName] == _manifest.PackageName)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenManifestIsStoredAndMetadataIsSet_SetsProjectStateTagAsInitialising()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        await _testee.StoreManifestAsync(internalManifest);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._,
                                                 A<Dictionary<string, string>>.That.Matches(dict => dict[Tags.ProjectState] == ProjectStates.Initialising)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreManifestAsync_WhenTagsAreNotSet_DeletesManifestBlob()
    {
        const string username = "TEST USERNAME";
        var internalManifest = new InternalManifest(_manifest, username);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>._)).Throws<Exception>();

        try
        {
            await _testee.StoreManifestAsync(internalManifest);
        }
        catch (Exception)
        { }

        A.CallTo(() => _blobService.DeleteAsync(A<string>.That.Matches(s => s.Contains(_manifest.ProjectId.ToString())))).MustHaveHappenedOnceExactly();
    }
}
