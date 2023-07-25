using FluentAssertions;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.AzureStorage.Specs.TableEntities;

[TestClass]
public class ResourceTableEntitySpecs
{
    private const string _fullName = "TEST FULL NAME";
    private const string _lockedTo = "TEST LOCKED TO";
    private const string _username = "user@test.com";
    private const string _versionId = "TEST VERSION ID";
    private const int _versionIndex = 19;
    private readonly Guid _projectId = Guid.NewGuid();

    [TestMethod]
    public void ToResource_ReturnsObjectWithExpectedPropertyValues()
    {
        var resourceTableEntity = new ResourceTableEntity
        {
            FullName = _fullName,
            LockedTo = _lockedTo,
            ProjectId = _projectId,
            Username = _username,
            VersionId = _versionId,
            VersionIndex = _versionIndex
        };

        var resource = resourceTableEntity.ToResource();

        resource.FullName.Should().Be(_fullName);
        resource.LockedTo.Should().Be(_lockedTo);
        resource.Username.Should().Be(_username);
        resource.VersionId.Should().Be(_versionId);
        resource.VersionIndex.Should().Be(_versionIndex);
    }
}
