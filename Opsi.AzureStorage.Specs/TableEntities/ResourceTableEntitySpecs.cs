using FluentAssertions;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.AzureStorage.Specs.TableEntities;

[TestClass]
public class ResourceTableEntitySpecs
{
    private const string _assignedBy = "TEST ASSIGNED BY";
    private const string _assignedTo = "TEST ASSIGNED TO";
    private const string _fullName = "TEST FULL NAME";
    private const string _username = "user@test.com";
    private const string _versionId = "TEST VERSION ID";
    private const int _versionIndex = 19;
    private DateTime _assignedOnUtc = DateTime.UtcNow;
    private readonly Guid _projectId = Guid.NewGuid();

    [TestMethod]
    public void ToResource_ReturnsObjectWithExpectedPropertyValues()
    {
        var resourceTableEntity = new ResourceTableEntity
        {
            AssignedBy = _assignedBy,
            AssignedOnUtc = _assignedOnUtc,
            AssignedTo = _assignedTo,
            FullName = _fullName,
            ProjectId = _projectId,
            CreatedBy = _username
        };

        var resource = resourceTableEntity.ToResource();

        resource.FullName.Should().Be(_fullName);
        resource.AssignedBy.Should().Be(_assignedBy);
        resource.AssignedOnUtc.Should().Be(_assignedOnUtc);
        resource.AssignedTo.Should().Be(_assignedTo);
        resource.CreatedBy.Should().Be(_username);
    }
}
