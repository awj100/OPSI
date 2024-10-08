﻿using Opsi.AzureStorage.Types.KeyPolicies;

namespace Opsi.AzureStorage.KeyPolicies;

public interface IResourceKeyPolicies
{
    KeyPolicy GetKeyPolicyForResourceHistory(Guid projectId);

    KeyPolicy GetKeyPolicyForResourceHistory(Guid projectId, string fullName);

    KeyPolicy GetKeyPolicyForResourceCount(Guid projectId, string fullName);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForNewVersion(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForStore(Guid projectId, string fullName, int versionIndex);

    IReadOnlyCollection<KeyPolicy> GetKeyPoliciesForUserAssignment(Guid projectId, string fullName, string assigneeUsername);
}
