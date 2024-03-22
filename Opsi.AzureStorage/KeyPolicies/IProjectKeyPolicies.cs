using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IProjectKeyPolicies
{
    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesByState(string projectState);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForUserAssignment(Guid projectId, string assigneeUsername, string resourceFullName);

    KeyPolicy GetKeyPolicyByState(string projectState, string keyOrder);

    KeyPolicy GetKeyPolicyForGetById(Guid projectId);

    KeyPolicy GetKeyPolicyByProjectForUserAssignment(Guid projectId, string assigneeUsername);

    KeyPolicy GetKeyPolicyByProjectForUserAssignment(Guid projectId, string assigneeUsername, string resourceFullName);

    KeyPolicy GetKeyPolicyByUserForUserAssignment(Guid projectId, string assigneeUsername, string resourceFullName);
}
