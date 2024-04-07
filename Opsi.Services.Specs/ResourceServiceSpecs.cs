using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.Types;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ResourceServiceSpecs
{
    private static Random random = new Random();
    private const string ContentTypeTextPlain = "text/plain";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private readonly Guid _projectId = Guid.NewGuid();
    private const string RemoteUriAsString = "https://a.url.com";
    private const string RestOfPath = "rest/of/path";
    private const string Username = "test@username";
    private const int VersionIndex = 123;
    private VersionInfo _versionInfo = new(VersionIndex);

    private IBlobService _blobService;
    private IWebhookQueueService _queueService;
    private ILoggerFactory _loggerFactory;
    private IProjectsService _projectsService;
    private IResourcesService _resourcesService;
    private ResourceStorageInfo _resourceStorageInfo;
    private VersionedResourceStorageInfo _versionedResourceStorageInfo;
    private IUserProvider _userProvider;
    private ResourceService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _blobService = A.Fake<IBlobService>();
        _queueService = A.Fake<IWebhookQueueService>();
        _loggerFactory = new NullLoggerFactory();
        _projectsService = A.Fake<IProjectsService>();
        _resourcesService = A.Fake<IResourcesService>();
        _userProvider = A.Fake<IUserProvider>();



        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.RestOfPath))))
            .Returns(_versionInfo);

        A.CallTo(() => _projectsService.GetWebhookSpecificationAsync(_projectId)).Returns(new ConsumerWebhookSpecification { Uri = RemoteUriAsString });
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => Username));

        _resourceStorageInfo = new ResourceStorageInfo(_projectId,
                                                       RestOfPath,
                                                       new MemoryStream(),
                                                       Username);

        _versionedResourceStorageInfo = _resourceStorageInfo.ToVersionedResourceStorageInfo(_versionInfo);

        _testee = new ResourceService(_resourcesService,
                                      _blobService,
                                      _queueService,
                                      _projectsService,
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
        A.CallTo(() => _userProvider.IsAdministrator).Returns(new Lazy<bool>(() => true));
        A.CallTo(() => _resourcesService.HasUserAccessAsync(_projectId, RestOfPath, Username)).Returns(false);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath, Username);


        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task HasUserAccessAsync_WhenUserIsNotAdminAndHasAccess_ReturnsTrue()
    {
        A.CallTo(() => _userProvider.IsAdministrator).Returns(new Lazy<bool>(() => false));
        A.CallTo(() => _resourcesService.HasUserAccessAsync(_projectId, RestOfPath, Username)).Returns(true);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath, Username);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task HasUserAccessAsync_WhenUserIsNotAdminAndHasNoAccess_ReturnsFalse()
    {
        A.CallTo(() => _userProvider.IsAdministrator).Returns(new Lazy<bool>(() => false));
        A.CallTo(() => _resourcesService.HasUserAccessAsync(_projectId, RestOfPath, Username)).Returns(false);

        var result = await _testee.HasUserAccessAsync(_projectId, RestOfPath, Username);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task StoreResourceAsync_DeterminesVersionInfo()
    {
        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.RestOfPath))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsLockedToAnotherUser_ThrowsException()
    {
        _versionInfo.AssignedTo = Option<string>.Some($"another_{Username}");

        A.CallTo(() => _resourcesService.GetCurrentVersionInfo(A<Guid>.That.Matches(g => g.Equals(_resourceStorageInfo.ProjectId)),
                                                               A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.RestOfPath))))
            .Returns(_versionInfo);

        await _testee.Invoking(y => y.StoreResourceAsync(_resourceStorageInfo))
                     .Should()
                     .ThrowAsync<ResourceLockConflictException>();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_StoredResourceHasIncrementedVersionIndex()
    {
        const int incrementedVersionIndex = VersionIndex + 1;

        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<VersionedResourceStorageInfo>.That.Matches(vrsi => vrsi.VersionInfo.Index == incrementedVersionIndex)))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreResourceAsync_WhenResourceIsStored_BlobVersionIsStoredInResourcesTable()
    {
        var blobVersion = Guid.NewGuid().ToString();

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(A<VersionedResourceStorageInfo>._)).Returns(blobVersion);

        await _testee.StoreResourceAsync(_resourceStorageInfo);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<VersionedResourceStorageInfo>.That.Matches(vrsi => vrsi.VersionId == blobVersion))).MustHaveHappenedOnceExactly();
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

    [TestMethod]
    public async Task StoreResourceAsync_WhenStoringBlobVersionFails_BlobIsDeleted()
    {
        var blobVersion = Guid.NewGuid().ToString();

        A.CallTo(() => _blobService.StoreVersionedResourceAsync(A<VersionedResourceStorageInfo>._)).Returns(blobVersion);

        A.CallTo(() => _resourcesService.StoreResourceAsync(A<VersionedResourceStorageInfo>._)).Throws<Exception>();

        await _testee.Invoking(y => y.StoreResourceAsync(_resourceStorageInfo))
                     .Should()
                     .ThrowAsync<Exception>();

        A.CallTo(() => _blobService.DeleteAsync(A<string>.That.Matches(s => s.Equals(_resourceStorageInfo.FullPath.Value)))).MustHaveHappenedOnceExactly();
    }

    private string GenerateRandomString(int length)
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

    private class TestBlobClient : BlobBaseClient
    {
        private readonly bool _exists;
        private readonly string _name;

        public TestBlobClient(string name, bool exists)
        {
            _exists = exists;
            _name = name;
        }

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
