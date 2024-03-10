using FluentAssertions;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Pocos;

namespace Opsi.AzureStorage.Specs.TableEntities;

[TestClass]
public class UserAssignmentTableEntitySpecs
{
    private const string AssignedByUsername = "TEST ASSIGNED BY USERNAME";
    private readonly DateTime AssignedOnUtc = DateTime.UtcNow;
    private const string AssigneeUsername = "TEST ASSIGNEE USERNAME";
    private const string PartitionKey = "TEST PARTITION KEY";
    private readonly Guid ProjectId = Guid.NewGuid();
    private const string ProjectName = "PROJECT NAME";
    private const string ResourceFullName = "TEST RESOURCE FULL NAME";
    private const string RowKey1 = "TEST ROW KEY 1";
    private const string RowKey2 = "TEST ROW KEY 2";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReadOnlyCollection<KeyPolicy> _keyPolicies;
    private KeyPolicy _keyPolicy1;
    private KeyPolicy _keyPolicy2;
    private UserAssignment _userAssignment;
    private UserAssignmentTableEntity _userAssignmentTableEntity;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _keyPolicy1 = new KeyPolicy(PartitionKey, new RowKey(RowKey1, KeyPolicyQueryOperators.Equal));
        _keyPolicy2 = new KeyPolicy(PartitionKey, new RowKey(RowKey2, KeyPolicyQueryOperators.Equal));
        _keyPolicies = new List<KeyPolicy> { _keyPolicy1, _keyPolicy2 };
        _userAssignment = new UserAssignment
        {
            AssignedByUsername = AssignedByUsername,
            AssignedOnUtc = AssignedOnUtc,
            AssigneeUsername = AssigneeUsername,
            ProjectId = ProjectId,
            ProjectName = ProjectName,
            ResourceFullName = ResourceFullName
        };
        _userAssignmentTableEntity = new UserAssignmentTableEntity
        {
            AssignedByUsername = AssignedByUsername,
            AssignedOnUtc = AssignedOnUtc,
            AssigneeUsername = AssigneeUsername,
            PartitionKey = PartitionKey,
            ProjectId = ProjectId,
            ProjectName = ProjectName,
            ResourceFullName = ResourceFullName,
            RowKey = RowKey1
        };
    }

    [TestMethod]
    public void FromUserAssignment_WhenSingleKeyPolicySpecified_ReturnsCorrectPropertyValues()
    {
        var tableEntity = UserAssignmentTableEntity.FromUserAssignment(_userAssignment, _keyPolicy1);

        tableEntity.AssignedByUsername.Should().Be(AssignedByUsername);
        tableEntity.AssignedOnUtc.Should().Be(AssignedOnUtc);
        tableEntity.AssigneeUsername.Should().Be(AssigneeUsername);
        tableEntity.PartitionKey.Should().Be(_keyPolicy1.PartitionKey);
        tableEntity.ProjectId.Should().Be(ProjectId);
        tableEntity.ProjectName.Should().Be(ProjectName);
        tableEntity.ResourceFullName.Should().Be(ResourceFullName);
        tableEntity.RowKey.Should().Be(_keyPolicy1.RowKey.Value);
    }

    [TestMethod]
    public void FromUserAssignment_WhenMultipleKeyPolicySpecified_ReturnsSameCountOfObjects()
    {
        var tableEntities = UserAssignmentTableEntity.FromUserAssignment(_userAssignment, _keyPolicies);

        tableEntities.Count.Should().Be(_keyPolicies.Count);
    }

    [TestMethod]
    public void FromUserAssignment_WhenMultipleKeyPolicySpecified_ReturnsObjectsWithSpecifiedKeys()
    {
        var tableEntities = UserAssignmentTableEntity.FromUserAssignment(_userAssignment, _keyPolicies);
        
        foreach (var keyPolicy in _keyPolicies)
        {
            tableEntities.Should().Contain(userAssignment => userAssignment.PartitionKey.Equals(keyPolicy.PartitionKey)
                                                             && userAssignment.RowKey.Equals(keyPolicy.RowKey.Value));
        }
    }

    [TestMethod]
    public void FromUserAssignment_WhenMultipleKeyPolicySpecified_ReturnsObjectsWithClonedPropertyValues()
    {
        var tableEntities = UserAssignmentTableEntity.FromUserAssignment(_userAssignment, _keyPolicies);

        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.AssignedByUsername.Should().Be(AssignedByUsername));
        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.AssignedOnUtc.Should().Be(AssignedOnUtc));
        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.AssigneeUsername.Should().Be(AssigneeUsername));
        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.ProjectId.Should().Be(ProjectId));
        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.ProjectName.Should().Be(ProjectName));
        tableEntities.Should().AllSatisfy(userAssignment => userAssignment.ResourceFullName.Should().Be(ResourceFullName));
    }

    [TestMethod]
    public void ToUserAssignment_ReturnsCorrectPropertyValues()
    {
        var userAssignment = _userAssignmentTableEntity.ToUserAssignment();

        userAssignment.Should().NotBeNull();
        userAssignment.AssignedByUsername.Should().Be(AssignedByUsername);
        userAssignment.AssignedOnUtc.Should().Be(AssignedOnUtc);
        userAssignment.AssigneeUsername.Should().Be(AssigneeUsername);
        userAssignment.ProjectId.Should().Be(ProjectId);
        userAssignment.ProjectName.Should().Be(ProjectName);
        userAssignment.ResourceFullName.Should().Be(ResourceFullName);
    }
}
