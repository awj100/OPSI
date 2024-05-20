using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.Types;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Metadata = Opsi.Constants.Metadata;

namespace Opsi.Services.Specs;

[TestClass]
public class ResourceServiceSpecs
{
    private static readonly Random random = new();
    private const string ContentTypeTextPlain = "text/plain";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private readonly Guid _projectId = Guid.NewGuid();
    private const string RemoteUriAsString = "https://a.url.com";
    private const string RestOfPath = "rest/of/path";
    private const string Username = "test@username";

    private IBlobService _blobService;
    private IManifestService _manifestService;
    private IWebhookQueueService _queueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private ResourceStorageInfo _resourceStorageInfo;
    private IUserProvider _userProvider;
    private ResourceService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        const string manifestName = "MANIFEST NAME";

        _blobService = A.Fake<IBlobService>();
        _queueService = A.Fake<IWebhookQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _manifestService = A.Fake<IManifestService>();
        _projectsService = A.Fake<IProjectsService>();
        _userProvider = A.Fake<IUserProvider>();

        _resourceStorageInfo = new ResourceStorageInfo(_projectId,
                                                       RestOfPath,
                                                       new MemoryStream());

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _projectsService.GetWebhookSpecificationAsync(_projectId)).Returns(new ConsumerWebhookSpecification { Uri = RemoteUriAsString });
        A.CallTo(() => _userProvider.Username).Returns(Username);
        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(new Dictionary<string, string> {
            {Metadata.Assignee, Username }
        });
        A.CallTo(() => _blobService.RetrieveTagsAsync(manifestName, A<bool>._)).Returns(new Dictionary<string, string> {
            { Tags.ProjectState, ProjectStates.InProgress }
        });
        A.CallTo(() => _userProvider.Username).Returns(Username);

        _testee = new ResourceService(_blobService,
                                      _queueService,
                                      _projectsService,
                                      _manifestService,
                                      _userProvider,
                                      _loggerFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _resourceStorageInfo.ContentStream?.Dispose();
    }

    [TestMethod]
    public async Task GetResourceContentAsync_WhenBlobExists_ReturnsOptionOfSomeWithCorrectValues()
    {
        const string filename = "test-file-name.ext";
        var blobFullName = $"folder/{filename}";
        var blobContentLength = random.Next(500);
        var blobContents = GenerateRandomString(blobContentLength);

        using var contentsStream = new MemoryStream(Encoding.UTF8.GetBytes(blobContents));

        var blobClient = new TestBlobClient(blobFullName, true)
        {
            ContentLength = blobContentLength,
            Contents = contentsStream
        };

        A.CallTo(() => _blobService.RetrieveBlobClient(A<string>._)).Returns(blobClient);

        var result = await _testee.GetResourceContentAsync(_projectId, RestOfPath);

        result.IsSome.Should().BeTrue();
        result.Value.Contents.Should().NotBeNull();
        result.Value.Contents.Length.Should().Be(blobContentLength);
        result.Value.Name.Should().Be(blobFullName);
        result.Value.Length.Should().Be(blobContentLength);
        result.Value.ContentType.Should().Be(ContentTypeTextPlain);
    }

    [TestMethod]
    public async Task GetResourceContentAsync_WhenNoBlobExists_ReturnsOptionOfNone()
    {
        var testBlobClient = new TestBlobClient(String.Empty, false);
        A.CallTo(() => _blobService.RetrieveBlobClient(A<string>._)).Returns(testBlobClient);

        var result = await _testee.GetResourceContentAsync(_projectId, RestOfPath);

        result.IsNone.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetResourceContentAsync_WhenBlobExists_IssuesWebhookWithResourceName()
    {
        const string filename = "test-file-name.ext";
        var blobFullName = $"folder/{filename}";
        var blobContentLength = random.Next(500);
        var blobContents = GenerateRandomString(blobContentLength);

        using var contentsStream = new MemoryStream(Encoding.UTF8.GetBytes(blobContents));

        var blobClient = new TestBlobClient(blobFullName, true)
        {
            ContentLength = blobContentLength,
            Contents = contentsStream
        };
        var webhookSpec = new ConsumerWebhookSpecification();

        A.CallTo(() => _blobService.RetrieveBlobClient(A<string>._)).Returns(blobClient);
        A.CallTo(() => _projectsService.GetWebhookSpecificationAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(webhookSpec);

        var result = await _testee.GetResourceContentAsync(_projectId, RestOfPath);

        A.CallTo(() => _queueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(filename)), webhookSpec)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task HasUserAccessAsync_WhenUserIsAdmin_ReturnsTrue()
    {
        A.CallTo(() => _userProvider.IsAdministrator).Returns(true);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task HasUserAccessAsync_WhenUserIsNotAdminAndHasAccess_ReturnsTrue()
    {
        A.CallTo(() => _userProvider.IsAdministrator).Returns(false);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task HasUserAccessAsync_WhenUserIsNotAdminAndHasNoAccess_ReturnsFalse()
    {
        const string differentUsername = "DIFFERENT USERNAME";

        A.CallTo(() => _userProvider.IsAdministrator).Returns(false);
        A.CallTo(() => _userProvider.Username).Returns(differentUsername);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task StoreResourceAsync_StoresAsNewVersion()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(_resourceStorageInfo)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_SetsCreatedByMetadataOnNewVersion()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _blobService.SetMetadataAsync(_resourceStorageInfo.BlobName.Value,
                                                     A<Dictionary<string, string>>.That.Matches(d => d.ContainsKey(Metadata.CreatedBy)
                                                     && d[Metadata.CreatedBy].Equals(_userProvider.Username))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_SetsProjectIdMetadataOnNewVersion()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.BlobName.Value)),
                                                     A<Dictionary<string, string>>.That.Matches(d => d.ContainsKey(Metadata.ProjectId)
                                                                                                     && d[Metadata.ProjectId].Equals(_resourceStorageInfo.ProjectId.ToString()))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_SetsProjectIdTagOnNewVersion()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.BlobName.Value)),
                                                 A<Dictionary<string, string>>.That.Matches(d => d.ContainsKey(Tags.ProjectId)
                                                                                                 && d[Tags.ProjectId].Equals(_resourceStorageInfo.ProjectId.ToString()))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenSettingMetadataFails_DeletesBlobVersionAndRethrowsException()
    {
        const string exceptionMessage = "EXCEPTION MESSAGE";
        const string versionId = "VERSION ID";

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(_resourceStorageInfo)).Returns(versionId);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).Throws(new Exception(exceptionMessage));

        await _testee.Invoking(t => t.StoreResourceAsync(_resourceStorageInfo)).Should().ThrowAsync<Exception>();

        A.CallTo(() => _blobService.DeleteVersionAsync(A<string>._, versionId)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenSettingTagsFails_DeletesBlobVersionAndRethrowsException()
    {
        const string exceptionMessage = "EXCEPTION MESSAGE";
        const string versionId = "VERSION ID";

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(_resourceStorageInfo)).Returns(versionId);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>._)).Throws(new Exception(exceptionMessage));

        await _testee.Invoking(t => t.StoreResourceAsync(_resourceStorageInfo)).Should().ThrowAsync<Exception>();

        A.CallTo(() => _blobService.DeleteVersionAsync(A<string>._, versionId)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenQueuingWebhookMessageFails_DeletesBlobVersionAndRethrowsException()
    {
        const string exceptionMessage = "EXCEPTION MESSAGE";
        const string versionId = "VERSION ID";

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(_resourceStorageInfo)).Returns(versionId);

        A.CallTo(() => _queueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).Throws(new Exception(exceptionMessage));

        await _testee.Invoking(t => t.StoreResourceAsync(_resourceStorageInfo)).Should().ThrowAsync<Exception>();

        A.CallTo(() => _blobService.DeleteVersionAsync(A<string>._, versionId)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsLockedToAnotherUser_ThrowsException()
    {
        const string differentUser = "DIFFERENT USER";

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(new Dictionary<string, string> {
            {Metadata.Assignee, differentUser }
        });

        await _testee.Invoking(t => t.StoreResourceAsync(_resourceStorageInfo))
                     .Should()
                     .ThrowAsync<UnassignedToResourceException>();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_WebhookUriIsRetrievedByProjectId()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _projectsService.GetWebhookSpecificationAsync(_projectId)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_WebhookMessageIsQueuedWithCorrectProjectId()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _queueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_projectId)), A<ConsumerWebhookSpecification>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_WebhookMessageIsQueuedWithCorrectRemoteUri()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _queueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                                       && cws.Uri != null
                                                                                                                                       && cws.Uri.Equals(RemoteUriAsString)))).MustHaveHappenedOnceExactly();
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static Action<T, TValue>? GetSetterForProperty<T, TValue>(Expression<Func<T, TValue>> selector) where T : class
    {
        var expression = selector.Body;
        var propertyInfo = expression.NodeType == ExpressionType.MemberAccess ? (PropertyInfo)((MemberExpression)expression).Member : null;

        if (propertyInfo is null)
        {
            return null;
        }

        var setter = GetPropertySetter(propertyInfo);

        return setter;

        static Action<T, TValue> GetPropertySetter(PropertyInfo prop)
        {
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter is not null)
            {
                return (obj, value) => setter.Invoke(obj, new object?[] { value });
            }

            var backingField = prop.DeclaringType?.GetField($"<{prop.Name}>k__BackingField", DeclaredOnlyLookup);
            if (backingField is null)
            {
                throw new InvalidOperationException($"Could not find a way to set {prop.DeclaringType?.FullName}.{prop.Name}. Try adding a private setter.");
            }

            return (obj, value) => backingField.SetValue(obj, value);
        }
    }

    private class TestBlobClient(string _name, bool _exists) : BlobBaseClient
    {
        public override string Name => _name;

        public long ContentLength { get; set; }

        public Stream? Contents { get; set; }

        public string? ContentType { get; set; } = ContentTypeTextPlain;

        public ETag Etag { get; set; } = new ETag("0x1C038C9CCB1AA00");

        public DateTimeOffset LastModified { get; set; } = DateTime.UtcNow;

        public override async Task<Response> DownloadToAsync(Stream memoryStream)
        {
            Contents!.Position = 0;
            await Contents.CopyToAsync(memoryStream);

            return A.Fake<Response>();
        }

        public override Task<Response<bool>> ExistsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response.FromValue(_exists, A.Fake<Response>()));
        }

        public override Task<Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            var blobProps = new BlobProperties();

            var setterContentLength = GetSetterForProperty<BlobProperties, long>(x => x.ContentLength);
            setterContentLength?.Invoke(blobProps, ContentLength);

            var setterContentType = GetSetterForProperty<BlobProperties, string>(x => x.ContentType);
            setterContentType?.Invoke(blobProps, ContentType!);

            // var setterEtag = GetSetterForProperty<BlobProperties, Etag>(x => x.ETag);
            // setterContentType?.Invoke(blobProps, ContentType!);

            var setterLastModified = GetSetterForProperty<BlobProperties, DateTimeOffset>(x => x.LastModified);
            setterLastModified?.Invoke(blobProps, LastModified!);

            var response = A.Fake<Response>();
            return Task.FromResult(Response.FromValue(blobProps, response));
        }
    }
}
