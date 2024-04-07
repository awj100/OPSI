using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using FakeItEasy;
using FluentAssertions;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Common;
using Opsi.Common.Exceptions;
using Opsi.Constants;
using Opsi.Constants.Webhooks;
using Opsi.Pocos;
using Opsi.Services.QueueServices;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsServiceSpecs
{
    private const string _assignedByUsername = "TEST ASSIGNED BY USERNAME";
    private const string _assigneeUsername1 = "TEST ASSIGNEE USERNAME 1";
    private const string _assigneeUsername2 = "TEST ASSIGNEE USERNAME 2";
    private const string _continuationToken = "TEST CONTINUATION TOKEN";
    private const int _pageSize = 10;
    private const string _projectName = "TEST PROJECT NAME";
    private const string _resource1FullName = "TEST RESOURCE 1 FULL NAME";
    private const string _resource2FullName = "TEST RESOURCE 2 FULL NAME";
    private const string _state2 = "TEST STATE 2";
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
    private string _defaultOrderBy = OrderBy.Desc;
    private OrderedProject _orderedProject;
    private Project _project;
    private ProjectTableEntity _projectTableEntity;
    private IProjectsTableService _projectsTableService;
    private readonly string _state1 = ProjectStates.InProgress;
    private ILoggerFactory _loggerFactory;
    private IResourcesService _resourcesService;
    private ResourceTableEntity _resourceTableEntity;
    private ResourceVersionTableEntity _resourceVersionTableEntity1;
    private ITagUtilities _tagUtilities;
    private UserAssignment _userAssignmentResource1User1;
    private UserAssignment _userAssignmentResource1User2;
    private UserAssignment _userAssignmentResource2User1;
    private UserAssignment _userAssignmentResource2User2;
    private IUserProvider _userProvider;
    private UserAssignmentTableEntity _userAssignmentTableEntity1;
    private UserAssignmentTableEntity _userAssignmentTableEntity2;
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

        var basicKeyPolicy = new KeyPolicy("TEST PARTITION KEY", new RowKey("TEST ROW KEY", KeyPolicyQueryOperators.Equal));
        Func<Project, IReadOnlyCollection<KeyPolicy>> basicKeyPolicyResolver = project => new List<KeyPolicy> { basicKeyPolicy };

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

        _orderedProject = new OrderedProject
        {
            Id = _projectId,
            Name = _projectName
        };

        _project = new Project
        {
            Id = _projectId,
            Name = _projectName,
            State = _state1,
            Username = _username,
            WebhookSpecification = _webhookSpecs
        };

        _projectTableEntity = new ProjectTableEntity
        {
            EntityType = typeof(ProjectTableEntity).Name,
            EntityVersion = 1,
            Id = _projectId,
            Name = _projectName,
            PartitionKey = "TEST PARTITION KEY",
            RowKey = "TEST ROW KEY",
            State = _state1,
            Username = _username
        };

        _resourceTableEntity = new ResourceTableEntity
        {
            EntityType = typeof(ResourceTableEntity).Name,
            EntityVersion = 1,
            FullName = _resource1FullName,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            RowKey = "TEST ROW KEY",
            Username = _username
        };

        _tagUtilities = A.Fake<ITagUtilities>();
        A.CallTo(() => _tagUtilities.GetSafeTagValue(A<object>._)).ReturnsLazily((object o) => o?.ToString() ?? String.Empty);

        _userAssignmentTableEntity1 = new UserAssignmentTableEntity
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            EntityType = typeof(UserAssignmentTableEntity).Name,
            EntityVersion = 1,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            ProjectName = _projectName,
            RowKey = "TEST ROW KEY",
            ResourceFullName = _resource1FullName
        };
        _userAssignmentResource1User1 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            ProjectId = _projectId,
            ResourceFullName = _resource1FullName
        };

        _userAssignmentTableEntity2 = new UserAssignmentTableEntity
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername2,
            EntityType = typeof(UserAssignmentTableEntity).Name,
            EntityVersion = 1,
            PartitionKey = "TEST PARTITION KEY",
            ProjectId = _projectId,
            ProjectName = _projectName,
            RowKey = "TEST ROW KEY",
            ResourceFullName = _resource2FullName
        };
        _userAssignmentResource1User2 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername2,
            ProjectId = _projectId,
            ResourceFullName = _resource1FullName
        };
        _userAssignmentResource2User2 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername2,
            ProjectId = _projectId,
            ResourceFullName = _resource2FullName
        };
        _userAssignmentResource2User1 = new UserAssignment
        {
            AssignedByUsername = _assignedByUsername,
            AssignedOnUtc = _assignedOnUtc,
            AssigneeUsername = _assigneeUsername1,
            ProjectId = _projectId,
            ResourceFullName = _resource2FullName
        };

        _resourceVersionTableEntity1 = new ResourceVersionTableEntity
        {
            FullName = _resource1FullName,
            ProjectId = _projectId,
            Username = _assigneeUsername1,
            VersionId = Guid.NewGuid().ToString(),
            VersionIndex = 1
        };

        Option<Project> nullProject = Option<Project>.None();

        _loggerFactory = new NullLoggerFactory();
        _projectsTableService = A.Fake<IProjectsTableService>();
        _resourcesService = A.Fake<IResourcesService>();
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

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(_project.Id)).Returns(Option<Project>.Some(_project));
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Not.Matches(g => g.Equals(_project.Id)))).Returns(nullProject);
        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_project.Id)).Returns(new List<ITableEntity> {
                                                                                                                    _projectTableEntity,
                                                                                                                    _resourceTableEntity,
                                                                                                                    _resourceVersionTableEntity1,
                                                                                                                    _userAssignmentTableEntity1
                                                                                                                  });
        A.CallTo(() => _projectsTableService.UpdateProjectStateAsync(_project.Id, _state2)).ReturnsLazily(() =>
        {
            _project.State = _state2;
            return Option<ProjectTableEntity>.Some(ProjectTableEntity.FromProject(_project, basicKeyPolicyResolver).First());
        });
        A.CallTo(() => _userProvider.Username).Returns(new Lazy<string>(() => _username));

        _testee = new ProjectsService(_projectsTableService, _blobService, _tagUtilities, _userProvider, _webhookQueueService);
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectNameIsEmpty_ThrowsArgumentNullException()
    {
        _project.Name = String.Empty;

        await _testee.Invoking(t => t.InitProjectAsync(_project)).Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectUsernameIsEmpty_ThrowsArgumentNullException()
    {
        _project.Username = String.Empty;

        await _testee.Invoking(t => t.InitProjectAsync(_project)).Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_Passes1ByteStreamToBlobService()
    {
        const long expectedStreamLength = 1;

        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.StoreResourceAsync(A<string>._, A<Stream>.That.Matches(s => ((MemoryStream)s).Length == expectedStreamLength))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesProjectIdInMetadata()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.ProjectId)
                                                                                                                     && dict[Metadata.ProjectId] == _project.Id.ToString()))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesProjectNameInMetadata()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.ProjectName)
                                                                                                                     && dict[Metadata.ProjectName] == _project.Name))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValidAndSpecifiesWebhookUri_PassesWebhookUriInMetadata()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.WebhookUri)
                                                                                                                     && dict[Metadata.WebhookUri] == _project.WebhookSpecification!.Uri))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValidAndSpecifiesNoWebhookUri_PassesWebhookUriAsEmptyStringInMetadata()
    {
        ConsumerWebhookSpecification? webhookSpec = null;
        var expectedMetadataValue = String.Empty;

        _project.WebhookSpecification = webhookSpec;

        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.WebhookUri)
                                                                                                                     && dict[Metadata.WebhookUri] == expectedMetadataValue))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValidAndSpecifiesWebhookCustomProps_PassesWebhookSerialisedCustomPropsInMetadata()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.WebhookUri)
                                                                                                                     && dict[Metadata.WebhookUri] == _project.WebhookSpecification!.Uri))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValidAndSpecifiesNoWebhookCustomProps_PassesSerialisedEmptyCustomPropsInMetadata()
    {
        Dictionary<string, object> customProps = [];
        var serialisedCustomProps = System.Text.Json.JsonSerializer.Serialize(customProps);

        _project.WebhookSpecification!.CustomProps = customProps;

        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.WebhookCustomProps)
                                                                                                                     && dict[Metadata.WebhookCustomProps] == serialisedCustomProps))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesCreatedByUsernameInMetadata()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Metadata.CreatedBy)
                                                                                                                     && dict[Metadata.CreatedBy] == _project.Username))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesProjectIdInTags()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Tags.ProjectId)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => String.Equals(dict[Tags.ProjectId], _project.Id.ToString())))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesProjectNameInTags()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Tags.ProjectName)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => String.Equals(dict[Tags.ProjectName], _project.Name)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_PassesInitialStateInTags()
    {
        var expectedProjectState = ProjectStates.Initialising;

        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => dict.ContainsKey(Tags.ProjectState)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>.That.Matches(dict => String.Equals(dict[Tags.ProjectState], expectedProjectState)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenFailsToStoreBlob_NoWebhookIsSent()
    {
        A.CallTo(() => _blobService.StoreResourceAsync(A<string>._, A<Stream>._)).ThrowsAsync(new Exception());

        try
        {
            await _testee.InitProjectAsync(_project);
        }
        catch(Exception)
        {}

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenFailsToSetMetadata_NoWebhookIsSent()
    {
        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).ThrowsAsync(new Exception());

        try
        {
            await _testee.InitProjectAsync(_project);
        }
        catch(Exception)
        {}

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenFailsToSetMetadata_BlobIsDeleted()
    {
        A.CallTo(() => _blobService.SetMetadataAsync(A<string>._, A<Dictionary<string, string>>._)).ThrowsAsync(new Exception());

        try
        {
            await _testee.InitProjectAsync(_project);
        }
        catch(Exception)
        {}

        A.CallTo(() => _blobService.DeleteAsync(A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenFailsToSetTags_NoWebhookIsSent()
    {
        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>._)).ThrowsAsync(new Exception());

        try
        {
            await _testee.InitProjectAsync(_project);
        }
        catch(Exception)
        {}

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<InternalWebhookMessage>._)).MustNotHaveHappened();
        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenFailsToSetTags_BlobIsDeleted()
    {
        A.CallTo(() => _blobService.SetTagsAsync(A<string>._, A<Dictionary<string, string>>._)).ThrowsAsync(new Exception());

        try
        {
            await _testee.InitProjectAsync(_project);
        }
        catch(Exception)
        {}

        A.CallTo(() => _blobService.DeleteAsync(A<string>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectStateTextInEventProperty()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)
                                                                                                          && cm.Event.Contains(Events.Stored)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task InitProjectAsync_WhenProjectIsValid_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.InitProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenNoProjectWithCorrespondingIdIsFound_ThrowsArgumentException()
    {
        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_userAssignmentResource1User1.ProjectId)).Returns(Enumerable.Empty<ITableEntity>().ToList());

        await _testee.Invoking(t => t.AssignUserAsync(_userAssignmentResource1User1)).Should().ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_PassesUserAssignmentWithProjectNameFromRetrievedProject()
    {
        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _projectsTableService.AssignUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectName.Equals(_userAssignmentResource2User1.ProjectName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_userAssignmentResource2User1.ProjectId)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectName()
    {
        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_userAssignmentResource2User1.ProjectName)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectEvent()
    {
        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserAssigned)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectUsername()
    {
        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_userAssignmentResource2User1.AssignedByUsername)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithAssigneeUsernameInCustomProps()
    {
        const string propNameAssignedUsername = "assignedUsername";

        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                                                                                             && cws.CustomProps.ContainsKey(propNameAssignedUsername)
                                                                                                                                             && cws.CustomProps[propNameAssignedUsername].Equals(_userAssignmentResource2User1.AssigneeUsername))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithResourceFullNameInCustomProps()
    {
        const string propNameResourceFullName = "resourceFullName";

        await _testee.AssignUserAsync(_userAssignmentResource2User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                                                                                             && cws.CustomProps.ContainsKey(propNameResourceFullName)
                                                                                                                                             && cws.CustomProps[propNameResourceFullName].Equals(_userAssignmentResource2User1.ResourceFullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenResourceIsAssignedToAnotherUser_ThrowsUserAssignmentException()
    {
        await _testee.Invoking(t => t.AssignUserAsync(_userAssignmentResource1User2)).Should().ThrowAsync<UserAssignmentException>();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenResourceIsAssignedToSameUser_DoesNotAssignToSameUser()
    {
        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _projectsTableService.AssignUserAsync(A<UserAssignment>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task AssignUserAsync_WhenResourceIsAssignedToSameUser_DoesNotInvokeWebhook()
    {
        await _testee.AssignUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoTableEntitiesAreReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>(0);

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoUserAssignmentTableEntityIsReturned_ThrowsUnassignedToProjectException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<UnassignedToProjectException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenNoProjectTableEntityReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturnedButProjectIsNotInProgress_ThrowsProjectStateException()
    {
        _projectTableEntity.State = ProjectStates.Completed;

        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetAssignedProjectAsync(_projectId, _assigneeUsername1))
                     .Should()
                     .ThrowAsync<ProjectStateException>();
    }

    [TestMethod]
    public async Task GetAssignedProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturned_ReturnsProjectWithResources()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId, _assigneeUsername1)).Returns(tableEntities);

        var result = await _testee.GetAssignedProjectAsync(_projectId, _assigneeUsername1);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNullOrEmpty().And.HaveCount(1);
        result.Resources.Single().FullName.Should().Be(_resource1FullName);
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenNoUserAssignmentsFound_ReturnsEmptyList()
    {
        const string assigneeUsername = "TEST ASSIGNEE USERNAME";
        var userAssignmentTableEntities = new List<UserAssignmentTableEntity>(0);

        A.CallTo(() => _projectsTableService.GetAssignedProjectsAsync(assigneeUsername)).Returns(userAssignmentTableEntities);

        var result = await _testee.GetAssignedProjectsAsync(assigneeUsername);

        result.Should()
            .NotBeNull().And
            .BeEmpty();
    }

    [TestMethod]
    public async Task GetAssignedProjectsAsync_WhenUserAssignmentsFound_ReturnsMappedUserAssignmentsFromTableService()
    {
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        const string assignedByUsername1 = "TEST ASSIGNED BY USERNAME 1";
        const string assignedByUsername2 = "TEST ASSIGNED BY USERNAME 2";
        const string assigneeUsername = "TEST ASSIGNEE USERNAME";
        const string partitionKey = "TEST PARTITION KEY";
        const string rowKey1 = "TEST ROW KEY 1";
        const string rowKey2 = "TEST ROW KEY 2";
        const string projectName1 = "TEST PROJECT NAME 1";
        const string projectName2 = "TEST PROJECT NAME 2";
        const string resourceFullName1 = "TEST RESOURCE FULL NAME 1";
        const string resourceFullName2 = "TEST RESOURCE FULL NAME 2";

        var userAssignmentTableEntities = new List<UserAssignmentTableEntity>
        {
            UserAssignmentTableEntity.FromUserAssignment(new UserAssignment
                                                         {
                                                             AssignedByUsername = assignedByUsername1,
                                                             AssignedOnUtc = DateTime.UtcNow,
                                                             AssigneeUsername = assigneeUsername,
                                                             ProjectId = projectId1,
                                                             ProjectName = projectName1,
                                                             ResourceFullName = resourceFullName1
                                                         }, new KeyPolicy(partitionKey, new RowKey(rowKey1, KeyPolicyQueryOperators.Equal))),
            UserAssignmentTableEntity.FromUserAssignment(new UserAssignment
                                                         {
                                                             AssignedByUsername = assignedByUsername2,
                                                             AssignedOnUtc = DateTime.UtcNow,
                                                             AssigneeUsername = assigneeUsername,
                                                             ProjectId = projectId2,
                                                             ProjectName = projectName2,
                                                             ResourceFullName = resourceFullName2
                                                         }, new KeyPolicy(partitionKey, new RowKey(rowKey2, KeyPolicyQueryOperators.Equal)))
        };

        A.CallTo(() => _projectsTableService.GetAssignedProjectsAsync(assigneeUsername)).Returns(userAssignmentTableEntities);

        var result = await _testee.GetAssignedProjectsAsync(assigneeUsername);

        result.Should()
            .NotBeNullOrEmpty().And
            .HaveCount(userAssignmentTableEntities.Count);

        foreach (var userAssignmentTableEntity in userAssignmentTableEntities)
        {
            result.Should().Contain(ua => ua.AssignedByUsername.Equals(userAssignmentTableEntity.AssignedByUsername)
                                          && ua.AssignedOnUtc.Equals(userAssignmentTableEntity.AssignedOnUtc)
                                          && ua.AssigneeUsername.Equals(userAssignmentTableEntity.AssigneeUsername)
                                          && ua.ProjectId.Equals(userAssignmentTableEntity.ProjectId)
                                          && ua.ProjectName.Equals(userAssignmentTableEntity.ProjectName)
                                          && ua.ResourceFullName.Equals(userAssignmentTableEntity.ResourceFullName));
        }
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoTableEntitiesAreReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>(0);

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetProjectAsync(_projectId))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoProjectTableEntityReturned_ThrowsProjectNotFoundException()
    {
        var tableEntities = new List<ITableEntity>
        {
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        await _testee.Invoking(t => t.GetProjectAsync(_projectId))
                     .Should()
                     .ThrowAsync<ProjectNotFoundException>();
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenNoUserAssignmentTableEntityIsReturned_ReturnsProjectWithResourcesWhichHaveNoAssignment()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        var result = await _testee.GetProjectAsync(_projectId);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNull().And.HaveCount(1);
        result.Resources.Should().AllSatisfy(resource => resource.AssignedTo.Should().BeNullOrEmpty());
        result.Resources.Should().AllSatisfy(resource => resource.AssignedBy.Should().BeNullOrEmpty());
        result.Resources.Should().AllSatisfy(resource => resource.AssignedOnUtc.HasValue.Should().BeFalse());
    }

    [TestMethod]
    public async Task GetProjectAsync_WhenUserAssignmentAndProjectTableEntitiesReturned_ReturnsProjectWithResourcesWhichHaveAssignmentProperties()
    {
        var tableEntities = new List<ITableEntity>
        {
            _projectTableEntity,
            _resourceTableEntity,
            _userAssignmentTableEntity1
        };

        A.CallTo(() => _projectsTableService.GetProjectEntitiesAsync(_projectId)).Returns(tableEntities);

        var result = await _testee.GetProjectAsync(_projectId);

        result.Should().NotBeNull();
        result.Name.Should().Be(_projectName);
        result.Id.Should().Be(_projectId);
        result.Resources.Should().NotBeNull().And.HaveCount(1);
        result.Resources.Single().AssignedTo.Should().Be(_userAssignmentResource1User1.AssigneeUsername);
        result.Resources.Single().AssignedBy.Should().Be(_userAssignmentResource1User1.AssignedByUsername);
        result.Resources.Single().AssignedOnUtc.Should().Be(_userAssignmentResource1User1.AssignedOnUtc);
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
        A.CallTo(() => _blobService.RetrieveBlobMetadataAsync(A<string>._, A<bool>._)).Returns(new Dictionary<string, string>(0));

        var result = await _testee.GetWebhookSpecificationAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWebhookUriAsync_WhenMatchingProjectFoundWithNoWebhookUri_ReturnsNull()
    {
        _blobMetadata[Metadata.WebhookUri] = String.Empty;

        var result = await _testee.GetWebhookSpecificationAsync(_projectId);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsFalse()
    {
        var blobClient = new TestBlobClient(true);

        A.CallTo(() => _blobService.RetrieveBlobClient(A<string>._)).Returns(blobClient);

        var result = await _testee.IsNewProjectAsync(_project.Id);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsNewProjectAsync_WhenMatchingProjectFound_ReturnsTrue()
    {
        var blobClient = new TestBlobClient(false);

        A.CallTo(() => _blobService.RetrieveBlobClient(A<string>._)).Returns(blobClient);

        var result = await _testee.IsNewProjectAsync(_project.Id);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenNoProjectWithCorrespondingIdIsFound_ThrowsArgumentException()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.None());

        await _testee.Invoking(t => t.RevokeUserAsync(_userAssignmentResource1User1)).Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_PassesUserAssignmentWithProjectNameFromRetrievedProject()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _projectsTableService.RevokeUserAsync(A<UserAssignment>.That.Matches(ua => ua.ProjectName.Equals(_project.Name))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectId()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectProjectName()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Name.Equals(_project.Name)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectEvent()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Event.Equals(Events.UserRevoked)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    /*[TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectLevel()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignment);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Level.Equals(Levels.)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }*/

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithCorrectUsername()
    {
        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(wm => wm.Username.Equals(_userAssignmentResource1User1.AssignedByUsername)), A<ConsumerWebhookSpecification>._))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithAssigneeUsernameInCustomProps()
    {
        const string propNameAssignedUsername = "revokedUsername";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameAssignedUsername)
                                                                && cws.CustomProps[propNameAssignedUsername].Equals(_assigneeUsername1))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task RevokeUserAsync_WhenProjectWithCorrespondingIdIsFound_InvokesWebhookWithResourceFullNameInCustomProps()
    {
        const string propNameResourceFullName = "resourceFullName";

        A.CallTo(() => _projectsTableService.GetProjectByIdAsync(A<Guid>.That.Matches(g => g.Equals(_projectId)))).Returns(Option<Project>.Some(_project));

        await _testee.RevokeUserAsync(_userAssignmentResource1User1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
            A<ConsumerWebhookSpecification>.That.Matches(cws => cws.CustomProps != null
                                                                && cws.CustomProps.ContainsKey(propNameResourceFullName)
                                                                && cws.CustomProps[propNameResourceFullName].Equals(_resource1FullName))))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectToTableService()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _projectsTableService.StoreProjectAsync(_project)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenNameIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.Name = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.Name));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenNameIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.Name = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.Name)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenStateIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.State = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.State));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenStateIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.State = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.State)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenUsernameIsEmpty_ThrowsArgumentExceptionWithMeaningfulMessage()
    {
        _project.Username = String.Empty;

        await _testee.Invoking(t => t.StoreProjectAsync(_project))
                     .Should()
                     .ThrowAsync<ArgumentNullException>()
                     .WithParameterName(nameof(Project.Username));
    }

    [TestMethod]
    public async Task StoreProjectAsync_WhenUsernameIsEmpty_NoProjectIsPassedToTableService()
    {
        _project.Username = String.Empty;

        try
        {
            await _testee.StoreProjectAsync(_project);
        }
        catch (ArgumentNullException exception) when (exception.ParamName!.Equals(nameof(Project.Username)))
        {
        }

        A.CallTo(() => _projectsTableService.StoreProjectAsync(A<Project>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectProjectId()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)), A<ConsumerWebhookSpecification>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectCustomProps()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoreProjectAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenStateIsChanged_ReturnsTrue()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(true);

        var result = await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoMatchingProjectFoundById_ReturnsFalse()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(false);

        var result = await _testee.UpdateProjectStateAsync(_projectId, _state2);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoMatchingProjectFoundById_DoesNotInvokeWebhook()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(false);

        await _testee.UpdateProjectStateAsync(_projectId, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_WhenNoStateChange_DoesNotInvokeWebhook()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(false);

        await _testee.UpdateProjectStateAsync(_project.Id, _state1);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._, A<ConsumerWebhookSpecification>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectProjectId()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(true);

        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectStateTextInEventProperty()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(true);

        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>.That.Matches(cm => cm.ProjectId.Equals(_project.Id)
                                                                                                          && cm.Event.Contains(Events.StateChange)
                                                                                                          && cm.Event.Contains(_state2)),
                                                                     A<ConsumerWebhookSpecification>._))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectCustomProps()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(true);

        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && cws.CustomProps != null
                                                                                                                         && cws.CustomProps.Count == _webhookCustomProps.Count)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectStateAsync_InvokesWebhookWithCorrectRemoteUri()
    {
        A.CallTo(() => _blobService.SetTagAsync(A<string>.That.Matches(s => s.Contains(_projectId.ToString())),
                                                A<string>.That.Matches(s => s.Equals(Tags.ProjectState)),
                                                A<string>.That.Matches(s => s.Equals(_state2)))).Returns(true);

        await _testee.UpdateProjectStateAsync(_project.Id, _state2);

        A.CallTo(() => _webhookQueueService.QueueWebhookMessageAsync(A<WebhookMessage>._,
                                                                     A<ConsumerWebhookSpecification>.That.Matches(cws => cws != null
                                                                                                                         && !String.IsNullOrWhiteSpace(cws.Uri)
                                                                                                                         && cws.Uri.Equals(_webhookUri))))
            .MustHaveHappenedOnceExactly();
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
