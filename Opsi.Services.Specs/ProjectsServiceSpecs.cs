using Azure;
using Azure.Storage.Blobs.Specialized;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _assignedByUsername = "TEST ASSIGNED BY USERNAME";
    private const string _assigneeUsername1 = "TEST ASSIGNEE USERNAME 1";
    private const string _projectName = "TEST PROJECT NAME";
    private const string _resource1FullName = "TEST RESOURCE 1 FULL NAME";
    private const string _username = "TEST USERNAME";
    private const string _webhookCustomProp1Name = nameof(_webhookCustomProp1Name);
    private const string _webhookCustomProp1Value = nameof(_webhookCustomProp1Value);
    private const string _webhookCustomProp2Name = nameof(_webhookCustomProp2Name);
    private const int _webhookCustomProp2Value = 2;
    private const string _webhookUri = "https://a.test.url";
    private readonly Guid _projectId = Guid.NewGuid();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private DateTime _assignedOnUtc;
    private Dictionary<string, string> _blobMetadata;
    private IBlobService _blobService;
    private InternalManifest _manifest;
    private IManifestService _manifestService;
    private Project _project;
    private readonly string _state1 = ProjectStates.InProgress;
    private ITagUtilities _tagUtilities;
    private UserAssignment _userAssignmentResource1User1;
    private IUserProvider _userProvider;
    private IWebhookQueueService _webhookQueueService;
    private Dictionary<string, object> _webhookCustomProps;
    private ConsumerWebhookSpecification _webhookSpecs;
    private ProjectsService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _assignedOnUtc = DateTime.UtcNow;

        _blobService = A.Fake<IBlobService>();

        _webhookCustomProps = new Dictionary<string, object>
        {
            { _webhookCustomProp1Name, _webhookCustomProp1Value },
            { _webhookCustomProp2Name, _webhookCustomProp2Value },
        };

        _webhookSpecs = new ConsumerWebhookSpecification
        {
            CustomProps = _webhookCustomProps,
            Uri = _webhookUri
        };

        var manifest = new Manifest
        {
            PackageName = _projectName,
            ProjectId = _projectId,
            HandlerQueue = QueueHandlerNames.Zipped,
            WebhookSpecification = _webhookSpecs
        };
        _manifest = new InternalManifest(manifest, _username);

        _manifestService = A.Fake<IManifestService>();
        A.CallTo(() => _manifestService.RetrieveManifestAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(_manifest);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).ReturnsLazily((Guid projectId) => $"{projectId}/{Tags.ManifestName}");

        _project = new Project
        {
            Id = _projectId,
            Name = _projectName,
            State = _state1,
            Username = _username,
            WebhookSpecification = _webhookSpecs
        };

        _tagUtilities = A.Fake<ITagUtilities>();
        A.CallTo(() => _tagUtilities.GetSafeTagValue(A<object>._)).ReturnsLazily((object o) => o?.ToString() ?? String.Empty);

        _userAssignmentResource1User1 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            ProjectId = _projectId,
            ResourceFullName = _resource1FullName
        };

        Option<Project> nullProject = Option<Project>.None();

        _userProvider = A.Fake<IUserProvider>();
        _webhookQueueService = A.Fake<IWebhookQueueService>();

        _blobMetadata = new Dictionary<string, string> {
            {Metadata.CreatedBy, _username },
            {Metadata.ProjectId, _projectId.ToString()},
            {Metadata.ProjectName, _projectName},
            {Metadata.WebhookCustomProps, System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>{
                { _webhookCustomProp1Name, _webhookCustomProp1Value},
                {_webhookCustomProp2Name, _webhookCustomProp2Value}
            })},
            {Metadata.WebhookUri, _webhookUri}
        };
        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(_blobMetadata);

        A.CallTo(() => _userProvider.Username).Returns(_username);

        _testee = new ProjectsService(_blobService, _manifestService, _userProvider, _tagUtilities, _webhookQueueService);
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenNoProjectWithCorrespondingIdIsFound_ThrowsResourceNotFoundException()
    {
        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, true)).Throws((string fullName, bool shouldThrow) => new ResourceNotFoundException(_userAssignmentResource1User1.ProjectId, _userAssignmentResource1User1.ResourceFullName));

        await _testee.Invoking(t => t.AssignUserAsync(_userAssignmentResource1User1)).Should().ThrowAsync<ResourceNotFoundException>();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasExistingAssignment_AndAssignmentIsAnotherUser_ThrowsUserAssignmentException()
    {
        const string existingAssignedUsername = "ANOTHER USER";
        var existingMetadata = new Dictionary<string, string>
        {
            {Metadata.Assignee, existingAssignedUsername }
        };

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.Invoking(t => t.AssignUserAsync(_userAssignmentResource1User1)).Should().ThrowAsync<UserAssignmentException>();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasExistingAssignment_AndAssignmentIsSameUser_DoesNotSetMetadata()
    {
        var existingMetadata = new Dictionary<string, string>
        {
            {Metadata.Assignee, _userAssignmentResource1User1.AssigneeUsername }
        };

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasExistingAssignment_AndAssignmentIsSameUser_DoesNotInvokeWebhook()
    {
        var existingMetadata = new Dictionary<string, string>
        {
            {Metadata.Assignee, _userAssignmentResource1User1.AssigneeUsername }
        };

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_SetsAssigneeFromSpecifiedAssignmentInMetadata()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(d => d[Metadata.Assignee] == _userAssignmentResource1User1.AssigneeUsername))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_SetsAssignedByFromSpecifiedAssignmentInMetadata()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(d => d[Metadata.AssignedBy] == _userAssignmentResource1User1.AssignedByUsername))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_SetsMetadataOnBlobUsingAssignmentResourceName()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)), A<Dictionary<string, string>>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_SetsTagSafeAssigneeFromSpecifiedAssignmentInTag()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);
        A.CallTo(() => _tagUtilities.GetSafeTagValue(A<object?>._)).ReturnsLazily((object? obj) => obj?.ToString() ?? String.Empty);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetTagAsync(A<string>._,
                                                Tags.Assignee,
                                                A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.AssigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_SetsTagSafeAssigneeUsingAssignmentResourceName()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);
        A.CallTo(() => _tagUtilities.GetSafeTagValue(A<object?>._)).ReturnsLazily((object? obj) => obj?.ToString() ?? String.Empty);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                Tags.Assignee,
                                                A<string>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_InvokesWebhookWithCorrectProjectId()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_projectId)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_InvokesWebhookWithCorrectResourceName()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_InvokesWebhookWithAssigneeFromSpecifiedAssignment()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(ws => ws != null
                                                                                                                       && ws.CustomProps != null
                                                                                                                       && ws.CustomProps.ContainsKey("assignedUsername")
                                                                                                                       && ws.CustomProps["assignedUsername"].Equals(_userAssignmentResource1User1.AssigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_InvokesWebhookWithCorrectEvent()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserAssigned)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenTargetBlobHasNoExistingAssignments_InvokesWebhookWithCorrectWebhookUri()
    {
        var existingMetadata = new Dictionary<string, string>();

        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(existingMetadata);

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(ws => ws != null
                                                                                                                 && ws.Uri != null
                                                                                                                 && ws.Uri.Equals(_webhookUri))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenWebhookSpecIsNull_DoesNotCallWebhookQueueService()
    {
        _manifest.WebhookSpecification = null;

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenWebhookSpecUriIsNull_DoesNotCallWebhookQueueService()
    {
        _manifest.WebhookSpecification!.Uri = null;

        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoBlobIsFound_ThrowsProjectNotFoundException()
    {
        var emptyBlobsWithAttributesList = new List<BlobWithAttributes>(0);
        var emptyPageableResponse = new PageableResponse<BlobWithAttributes>(emptyBlobsWithAttributesList, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(emptyPageableResponse);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1)).Should().ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_AndNoUserIsAssigned_ThrowsUnassignedToProjectException()
    {
        const string blobName = "BLOB NAME";
        var blobWithAttributes = new BlobWithAttributes(blobName);
        var blobsWithAttributesList = new List<BlobWithAttributes> { blobWithAttributes };
        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributesList, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1)).Should().ThrowAsync<UnassignedToResourceException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_AndAnotherUserIsAssigned_ThrowsUnassignedToProjectException()
    {
        const string blobName = "BLOB NAME";
        var blobWithAttributes = new BlobWithAttributes(blobName);
        blobWithAttributes.Metadata[Metadata.Assignee] = "ANOTHER USER";
        var blobsWithAttributesList = new List<BlobWithAttributes> { blobWithAttributes };
        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributesList, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1)).Should().ThrowAsync<UnassignedToResourceException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_AndCorrespondingManifestIsNotFound_ThrowsManifestNotFoundException()
    {
        const string blobName = "BLOB NAME";
        var projectBlob = new BlobWithAttributes(blobName);
        projectBlob.Metadata[Metadata.Assignee] = _assigneeUsername1;
        var blobsWithAttributesList = new List<BlobWithAttributes> { projectBlob };
        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributesList, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1)).Should().ThrowAsync<ManifestNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_AndProjectStateIsNotInProgress_ThrowsProjectStateException()
    {
        const string createdBy = "CREATED BY";
        const string projectName = "PROJECT NAME";
        var projectState = ProjectStates.Initialising;

        var manifestName = "MANIFEST NAME";
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);
        var manifestBlob = new BlobWithAttributes(manifestName);
        manifestBlob.Metadata[Metadata.CreatedBy] = createdBy;
        manifestBlob.Metadata[Metadata.ProjectName] = projectName;
        manifestBlob.Tags[Tags.ProjectState] = projectState;

        const string blobName = "BLOB NAME";
        var projectBlob = new BlobWithAttributes(blobName);
        projectBlob.Metadata[Metadata.Assignee] = _assigneeUsername1;

        var blobsWithAttributesList = new List<BlobWithAttributes> {
                                                                       manifestBlob,
                                                                       projectBlob
                                                                   };
        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributesList, null);

        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1)).Should().ThrowAsync<ProjectStateException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_ReturnsProjectResourcesWithoutManifest()
    {
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateAllProjectResources(numberOfResources: 2,
                                                           _projectId,
                                                           _projectName,
                                                           ProjectStates.InProgress,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy);

        var manifestName = GenerateManifestName(_projectId);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Resources.Should().NotContain(resource => resource.FullName.Equals(manifestName));
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_ReturnsProjectWithExpectedResources()
    {
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateAllProjectResources(numberOfResources: 2,
                                                           _projectId,
                                                           _projectName,
                                                           ProjectStates.InProgress,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy);

        var manifestName = GenerateManifestName(_projectId);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Resources.Should().HaveCount(2);
        result.Resources.Should().AllSatisfy(resource =>
        {
            resource.Should().NotBeNull();
            resource.AssignedBy.Should().Be(assignedBy);
            resource.AssignedTo.Should().Be(_assigneeUsername1);
            resource.CreatedBy.Should().Be(createdBy);
            resource.ProjectId.Should().Be(_projectId);
        });
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_ReturnsProjectWithExpectedProjectId()
    {
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateAllProjectResources(numberOfResources: 2,
                                                           _projectId,
                                                           _projectName,
                                                           ProjectStates.InProgress,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy);

        var manifestName = GenerateManifestName(_projectId);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Id.Should().Be(_projectId);
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_ReturnsProjectWithExpectedProjectName()
    {
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateAllProjectResources(numberOfResources: 2,
                                                           _projectId,
                                                           _projectName,
                                                           ProjectStates.InProgress,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy);

        var manifestName = GenerateManifestName(_projectId);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenBlobIsFound_ReturnsProjectWithExpectedProjectState()
    {
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateAllProjectResources(numberOfResources: 2,
                                                           _projectId,
                                                           _projectName,
                                                           ProjectStates.InProgress,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy);

        var manifestName = GenerateManifestName(_projectId);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(manifestName);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.ProjectId,
                                                       A<string>.That.Matches(s => s.Equals(_projectId.ToString())),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.State.Should().Be(projectState);
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenNoProjectsAssigned_ReturnsPageableObjectWithEmptyItems()
    {
        var blobsWithAttributes = new List<BlobWithAttributes>(0);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.Assignee,
                                                       A<string>.That.Matches(s => s.Equals(_assigneeUsername1)),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectsAsync(_assigneeUsername1);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenProjectsAreAssigned_ReturnsPageableObjectWithExpectedNumberOfItems()
    {
        var project1Id = Guid.NewGuid();
        var project1Name = "PROJECT 1 NAME";
        var project2Id = Guid.NewGuid();
        var project2Name = "PROJECT 2 NAME";
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateProjectResources(project1Id,
                                                           project1Name,
                                                           projectState,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy).Take(2).ToList();

        var manifest1Blob = GenerateManifestBlob(project1Id,
                                                 createdBy,
                                                 project1Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project1Id)))).Returns(manifest1Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest1Blob.Name)), A<bool>._)).Returns(manifest1Blob.Tags);

        blobsWithAttributes.AddRange(GenerateProjectResources(project2Id,
                                                              project2Name,
                                                              projectState,
                                                              assignedBy,
                                                              _assigneeUsername1,
                                                              createdBy).Take(2).ToList());

        var manifest2Blob = GenerateManifestBlob(project2Id,
                                                 createdBy,
                                                 project2Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project2Id)))).Returns(manifest2Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest2Blob.Name)), A<bool>._)).Returns(manifest2Blob.Tags);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.Assignee,
                                                       A<string>.That.Matches(s => s.Equals(_assigneeUsername1)),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectsAsync(_assigneeUsername1);

        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenProjectsAreAssigned_ReturnsPageableObjectWithExpectedProjectIds()
    {
        var project1Id = Guid.NewGuid();
        var project1Name = "PROJECT 1 NAME";
        var project2Id = Guid.NewGuid();
        var project2Name = "PROJECT 2 NAME";
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateProjectResources(project1Id,
                                                           project1Name,
                                                           projectState,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy).Take(2).ToList();

        var manifest1Blob = GenerateManifestBlob(project1Id,
                                                 createdBy,
                                                 project1Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project1Id)))).Returns(manifest1Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest1Blob.Name)), A<bool>._)).Returns(manifest1Blob.Tags);

        blobsWithAttributes.AddRange(GenerateProjectResources(project2Id,
                                                              project2Name,
                                                              projectState,
                                                              assignedBy,
                                                              _assigneeUsername1,
                                                              createdBy).Take(2).ToList());

        var manifest2Blob = GenerateManifestBlob(project2Id,
                                                 createdBy,
                                                 project2Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project2Id)))).Returns(manifest2Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest2Blob.Name)), A<bool>._)).Returns(manifest2Blob.Tags);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.Assignee,
                                                       A<string>.That.Matches(s => s.Equals(_assigneeUsername1)),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectsAsync(_assigneeUsername1);

        result.Should().NotBeNullOrEmpty();
        result.Should().ContainSingle(project => project.Id.Equals(project1Id));
        result.Should().ContainSingle(project => project.Id.Equals(project2Id));
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenProjectsAreAssigned_ReturnsPageableObjectWithExpectedProjectNames()
    {
        var project1Id = Guid.NewGuid();
        var project1Name = "PROJECT 1 NAME";
        var project2Id = Guid.NewGuid();
        var project2Name = "PROJECT 2 NAME";
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var projectState = ProjectStates.InProgress;

        var blobsWithAttributes = GenerateProjectResources(project1Id,
                                                           project1Name,
                                                           projectState,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy).Take(2).ToList();

        var manifest1Blob = GenerateManifestBlob(project1Id,
                                                 createdBy,
                                                 project1Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project1Id)))).Returns(manifest1Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest1Blob.Name)), A<bool>._)).Returns(manifest1Blob.Tags);

        blobsWithAttributes.AddRange(GenerateProjectResources(project2Id,
                                                              project2Name,
                                                              projectState,
                                                              assignedBy,
                                                              _assigneeUsername1,
                                                              createdBy).Take(2).ToList());

        var manifest2Blob = GenerateManifestBlob(project2Id,
                                                 createdBy,
                                                 project2Name,
                                                 projectState);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project2Id)))).Returns(manifest2Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest2Blob.Name)), A<bool>._)).Returns(manifest2Blob.Tags);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.Assignee,
                                                       A<string>.That.Matches(s => s.Equals(_assigneeUsername1)),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectsAsync(_assigneeUsername1);

        result.Should().NotBeNullOrEmpty();
        result.Should().ContainSingle(project => project.Name.Equals(project1Name));
        result.Should().ContainSingle(project => project.Name.Equals(project2Name));
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenProjectsAreAssigned_ReturnsProjectsOnlyWithStatusInProgress()
    {
        var project1Id = Guid.NewGuid();
        var project1Name = "PROJECT 1 NAME";
        var project2Id = Guid.NewGuid();
        var project2Name = "PROJECT 2 NAME";
        const string assignedBy = "ASSIGNED BY";
        const string createdBy = "CREATED BY";
        var project1State = ProjectStates.InProgress;
        var project2State = ProjectStates.Initialising;

        var blobsWithAttributes = GenerateProjectResources(project1Id,
                                                           project1Name,
                                                           project1State,
                                                           assignedBy,
                                                           _assigneeUsername1,
                                                           createdBy).Take(2).ToList();

        var manifest1Blob = GenerateManifestBlob(project1Id,
                                                 createdBy,
                                                 project1Name,
                                                 project1State);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project1Id)))).Returns(manifest1Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest1Blob.Name)), A<bool>._)).Returns(manifest1Blob.Tags);

        blobsWithAttributes.AddRange(GenerateProjectResources(project2Id,
                                                              project2Name,
                                                              project2State,
                                                              assignedBy,
                                                              _assigneeUsername1,
                                                              createdBy).Take(2).ToList());

        var manifest2Blob = GenerateManifestBlob(project2Id,
                                                 createdBy,
                                                 project2Name,
                                                 project2State);
        A.CallTo(() => _manifestService.GetManifestFullName(A<Guid>.That.Matches(g => g.Equals(project2Id)))).Returns(manifest2Blob.Name);
        A.CallTo(() => _blobService.RetrieveTagsAsync(A<string>.That.Matches(s => s.Equals(manifest2Blob.Name)), A<bool>._)).Returns(manifest2Blob.Tags);

        var pageableResponse = new PageableResponse<BlobWithAttributes>(blobsWithAttributes, null);

        A.CallTo(() => _blobService.RetrieveByTagAsync(Tags.Assignee,
                                                       A<string>.That.Matches(s => s.Equals(_assigneeUsername1)),
                                                       A<int>._,
                                                       A<string?>._)).Returns(pageableResponse);

        var result = await _testee.GetAssignedProjectsAsync(_assigneeUsername1);

        result.Should().NotBeNullOrEmpty();
        result.Should().ContainSingle(project => project.State.Equals(project1State));
        result.Should().NotContain(project => project.State.Equals(project2State));
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhook()
    {
        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().NotBeNull();
        result!.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedUri()
    {
        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.Uri.Should().Be(_webhookUri);
    }

    [TestMethod]
    public async Task GetWebhookAsync_WhenMatchingProjectFound_ReturnsWebhookWithExpectedCustomProps()
    {
        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result?.CustomProps.Should().NotBeNullOrEmpty();
        result!.CustomProps.Should().HaveCount(_webhookCustomProps.Count);
        result!.CustomProps!.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp1Name);
        // result!.CustomProps![_webhookCustomProp1Name].Should().Be(_webhookCustomProp1Value);
        result!.CustomProps!.Select(keyValuePair => keyValuePair.Key).Should().Contain(_webhookCustomProp2Name);
        //result!.CustomProps[_webhookCustomProp2Name].Should().Be(_webhookCustomProp2Value);
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        A.CallTo(() => _manifestService.RetrieveManifestAsync(A<Guid>._)).Returns((InternalManifest?)null);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenMatchingProjectFoundWithNoWebhookUri_ReturnsNull()
    {
        _manifest.WebhookSpecification = null;
        A.CallTo(() => _manifestService.RetrieveManifestAsync(A<Guid>._)).Returns(_manifest);

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectNameIsEmpty_ThrowsArgumentException()
    {
        _manifest.PackageName = String.Empty;

        await _testee.Invoking(t => t.InitProjectAsync(_manifest)).Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectUsernameIsEmpty_ThrowsArgumentException()
    {
        _manifest.Username = String.Empty;

        await _testee.Invoking(t => t.InitProjectAsync(_manifest)).Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_StoresManifest()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _manifestService.StoreManifestAsync(A<InternalManifest>.That.Matches(m => m == _manifest))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValidAndSpecifiesWebhookUri_PassesWebhookUriInMetadata()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                        && cws.Uri != null
                                                                                                                        && cws.Uri.Equals(_webhookUri))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectStateTextInEventProperty()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)
                                                                                                          && cm.Event.Contains(Events.Stored)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.InitProjectAsync(_manifest);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsFalse()
    {
        var blobClient = new TestBlobClient(false);

        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);

        var result = await _testee.IsNewProjectAsync(_projectId);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenNoMatchingProjectFound_ReturnsTrue()
    {
        var blobClient = new TestBlobClient(false);

        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns((InternalManifest?)null);

        var result = await _testee.IsNewProjectAsync(_projectId);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsNotFound_ThrowsNoException()
    {
        A.CallTo(() => _blobService.RemovePropertiesAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                          A<IEnumerable<string>>._,
                                                          true))
         .Throws(new ResourceNotFoundException(Guid.Empty, _userAssignmentResource1User1.ResourceFullName));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_RemovesAssignedByMetadataFromBlob()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.RemovePropertiesAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                          A<IEnumerable<string>>.That.Contains(Metadata.AssignedBy),
                                                          A<bool>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_RemovesAssignedOnMetadataFromBlob()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.RemovePropertiesAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                          A<IEnumerable<string>>.That.Contains(Metadata.AssignedOnUtc),
                                                          A<bool>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_RemovesAssigneeMetadataFromBlob()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.RemovePropertiesAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                          A<IEnumerable<string>>.That.Contains(Metadata.Assignee),
                                                          A<bool>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_RemovesAssigneeTagFromBlob()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _blobService.RemoveTagAsync(A<string>.That.Matches(s => s.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                   Tags.Assignee,
                                                   A<bool>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsNull_MakesNoAttemptToSendWebhookMessage()
    {
        // Test with the WebhookSpecification.Uri as null.
        _manifest.WebhookSpecification!.Uri = null;

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();

        // Now test with the entire WebhookSpecification as null.
        _manifest.WebhookSpecification = null;

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsWebhookMessageToSpecifiedUri()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                        && cws.Uri != null
                                                                                                                        && cws.Uri.Equals(_webhookUri))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsUserRevokedEventInWebhookMessage()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserRevoked)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsResourceNameInWebhookMessage()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_userAssignmentResource1User1.ResourceFullName)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsResourceLevelInWebhookMessage()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Level.Equals(Levels.Resource)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsProjectIdInWebhookMessage()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_userAssignmentResource1User1.ProjectId)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenWebhookUriIsSpecified_SendsRevokedByUsernameInWebhookMessage()
    {
        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_userAssignmentResource1User1.AssignedByUsername)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenStateIsNotRecognised_ThrowsInvalidStateException()
    {
        const string invalidState = "INVALID STATE";

        await _testee.Invoking(t => t.UpdateProjectStateAsync(_project.Id, invalidState)).Should().ThrowAsync<InvalidProjectStateException>();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenProjectIsNotFound_ReturnsFalse()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(false);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenProjectIsFound_SetsNewStateOnManifestTag()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriNotSetOnManifest_MakesNoAttemptToSendWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        // Set the WebhookSpecification.Uri as null.
        _manifest.WebhookSpecification!.Uri = null;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();

        // Now set the WebhookSpecification as null.
        _manifest.WebhookSpecification = null;

        await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsWebhookMessageToSpecifiedUri()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                        && cws.Uri != null
                                                                                                                        && cws.Uri.Equals(_webhookUri))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsNewStateInWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Contains(newState)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsProjectIdInWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_projectId)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsProjectNameInWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_manifest.PackageName)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsProjectLevelInWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Level.Equals(Levels.Project)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenWebhookUriSetOnManifest_SendsUsernameInWebhookMessage()
    {
        const string manifestName = "MANIFEST NAME";
        var newState = ProjectStates.Completed;

        A.CallTo(() => _manifestService.GetManifestFullName(_projectId)).Returns(manifestName);
        A.CallTo(() => _manifestService.RetrieveManifestAsync(_projectId)).Returns(_manifest);
        A.CallTo(() => _blobService.SetTagAsync(manifestName, Tags.ProjectState, newState)).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_projectId, newState);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_username)),
                                                                     A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    private static string GenerateManifestName(Guid projectId)
    {
        return $"MANIFEST NAME {projectId}";
    }

    private static List<BlobWithAttributes> GenerateAllProjectResources(int numberOfResources,
                                                                        Guid projectId,
                                                                        string projectName,
                                                                        string projectState,
                                                                        string assignedBy,
                                                                        string assignee,
                                                                        string createdBy)
    {
        var blobsWithAttributes = new List<BlobWithAttributes>(numberOfResources + 1);

        var manifestBlob = GenerateManifestBlob(projectId,
                                                createdBy,
                                                projectName,
                                                projectState);

        blobsWithAttributes.Add(manifestBlob);

        blobsWithAttributes.AddRange(GenerateProjectResources(projectId,
                                                                 projectName,
                                                                 projectState,
                                                                 assignedBy,
                                                                 assignee,
                                                                 createdBy).Take(numberOfResources)
                                                                           .ToList());

        return blobsWithAttributes;
    }

    private static BlobWithAttributes GenerateManifestBlob(Guid projectId,
                                                           string createdBy,
                                                           string projectName,
                                                           string projectState)
    {
        var manifestName = GenerateManifestName(projectId);
        var manifestBlob = new BlobWithAttributes(manifestName);
        manifestBlob.Metadata[Metadata.CreatedBy] = createdBy;
        manifestBlob.Metadata[Metadata.ProjectName] = projectName;
        manifestBlob.Tags[Tags.ProjectId] = projectId.ToString();
        manifestBlob.Tags[Tags.ProjectName] = projectName;
        manifestBlob.Tags[Tags.ProjectState] = projectState;

        return manifestBlob;
    }

    private static IEnumerable<BlobWithAttributes> GenerateProjectResources(Guid projectId,
                                                                            string projectName,
                                                                            string projectState,
                                                                            string assignedBy,
                                                                            string assignee,
                                                                            string createdBy)
    {
        var i = 0;

        while (true)
        {
            var blobWithAttributes = new BlobWithAttributes($"{projectId}/BLOB {i++} NAME");
            blobWithAttributes.Metadata[Metadata.AssignedBy] = assignedBy;
            blobWithAttributes.Metadata[Metadata.AssignedOnUtc] = DateTime.UtcNow.ToString();
            blobWithAttributes.Metadata[Metadata.Assignee] = assignee;
            blobWithAttributes.Metadata[Metadata.CreatedBy] = createdBy;
            blobWithAttributes.Metadata[Metadata.ProjectId] = projectId.ToString();
            blobWithAttributes.Metadata[Metadata.ProjectName] = projectName;
            blobWithAttributes.Tags[Tags.Assignee] = assignee;
            blobWithAttributes.Tags[Tags.ProjectId] = projectId.ToString();
            blobWithAttributes.Tags[Tags.ProjectName] = projectName;
            blobWithAttributes.Tags[Tags.ProjectState] = projectState;

            yield return blobWithAttributes;
        }
    }

    private class TestBlobClient(bool exists) : BlobBaseClient
    {
        private readonly bool _exists = exists;

        public override Task<Response<bool>> ExistsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response.FromValue(_exists, A.Fake<Response>()));
        }
    }
}
