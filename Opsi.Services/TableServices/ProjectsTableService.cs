﻿using System.Reflection;
using Azure.Data.Tables;
using Opsi.AzureStorage;
using Opsi.AzureStorage.KeyPolicies;
using Opsi.AzureStorage.TableEntities;
using Opsi.AzureStorage.Types.KeyPolicies;
using Opsi.Common;
using Opsi.Pocos;

namespace Opsi.Services.TableServices;

internal class ProjectsTableService : IProjectsTableService
{
    private const string TableName = "resources";
    private readonly ITableService _projectsTableService;
    private readonly IProjectKeyPolicies _projectKeyPolicies;
    private readonly IResourceKeyPolicies _resourceKeyPolicies;
    private readonly IKeyPolicyFilterGeneration _keyPolicyFilterGeneration;

    public ProjectsTableService(IProjectKeyPolicies projectKeyPolicies,
                                IResourceKeyPolicies resourceKeyPolicies,
                                ITableServiceFactory tableServiceFactory,
                                IKeyPolicyFilterGeneration keyPolicyFilterGeneration)
    {
        _keyPolicyFilterGeneration = keyPolicyFilterGeneration;
        _projectKeyPolicies = projectKeyPolicies;
        _projectsTableService = tableServiceFactory.Create(TableName);
        _resourceKeyPolicies = resourceKeyPolicies;
    }

    public async Task AssignUserAsync(UserAssignment userAssignment)
    {
        var projectKeyPolicies = _projectKeyPolicies.GetKeyPoliciesForUserAssignment(userAssignment.ProjectId, userAssignment.AssigneeUsername);
        var resourceKeyPolicies = _resourceKeyPolicies.GetKeyPoliciesForUserAssignment(userAssignment.ProjectId, userAssignment.ResourceFullName, userAssignment.AssigneeUsername);

        var keyPolicies = new List<KeyPolicy>(projectKeyPolicies.Count + resourceKeyPolicies.Count);
        keyPolicies.AddRange(projectKeyPolicies);
        keyPolicies.AddRange(resourceKeyPolicies);

        var tableEntities = UserAssignmentTableEntity.FromUserAssignment(userAssignment, keyPolicies);

        await _projectsTableService.StoreTableEntitiesAsync(tableEntities);
    }

    public async Task<IReadOnlyCollection<UserAssignmentTableEntity>> GetAssignedProjectsAsync(string assigneeUsername)
    {
        const int maxResultsPerPage = 500;
        var propNamesToSelect = GetPropertyNames<UserAssignmentTableEntity>();
        var dummyProjectId = Guid.NewGuid();    // We will use only the partition key, and this project ID is required for a row key which we will neglect.

        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyByUserForUserAssignment(dummyProjectId, assigneeUsername);

        var tableClient = _projectsTableService.TableClient.Value;

        var results = tableClient.QueryAsync<UserAssignmentTableEntity>($"PartitionKey eq '{keyPolicyForGet.PartitionKey}'",
                                                                        maxPerPage: maxResultsPerPage,
                                                                        select: propNamesToSelect,
                                                                        cancellationToken: CancellationToken.None);

        var userAssignmentTableEntities = new List<UserAssignmentTableEntity>();
        await foreach (var userAssignmentTableEntity in results)
        {
            userAssignmentTableEntities.Add(userAssignmentTableEntity);
        }

        return userAssignmentTableEntities;
    }

    public async Task<Option<Project>> GetProjectByIdAsync(Guid projectId)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (projectTableEntity.IsSome)
        {
            return Option<Project>.Some(projectTableEntity.Value.ToProject());
        }

        return Option<Project>.None();
    }

    public async Task<PageableResponse<OrderedProject>> GetProjectsByStateAsync(string projectState, string orderBy, int pageSize, string? continuationToken = null)
    {
        var tableClient = _projectsTableService.TableClient.Value;
        var keyPolicies = _projectKeyPolicies.GetKeyPoliciesByState(projectState);
        var propNamesToSelect = GetPropertyNames<OrderedProjectTableEntity>();

        var keyPolicy = keyPolicies.SingleOrDefault(keyPolicy => keyPolicy.RowKey.Value.Contains(orderBy, StringComparison.OrdinalIgnoreCase));
        if (String.IsNullOrEmpty(keyPolicy.PartitionKey))
        {
            throw new Exception($"No key policy has been declared which matches the specified order-by (\"{orderBy}\").");
        }

        var pageResult = tableClient.QueryAsync<OrderedProjectTableEntity>($"PartitionKey eq '{keyPolicy.PartitionKey}'",
                                                                           maxPerPage: pageSize,
                                                                           select: propNamesToSelect,
                                                                           cancellationToken: CancellationToken.None);

        if (pageResult == null)
        {
            return new PageableResponse<OrderedProject>(new List<OrderedProject>(0));
        }

        await foreach (var page in pageResult.AsPages(continuationToken))
        {
            var orderedProjects = page.Values.Select(orderedProjectTableEntity => orderedProjectTableEntity.ToOrderedProject()).ToList();
            return new PageableResponse<OrderedProject>(orderedProjects, page.ContinuationToken);
        }

        return new PageableResponse<OrderedProject>(new List<OrderedProject>(0));
    }

    public async Task RevokeUserAsync(UserAssignment userAssignment)
    {
        var projectKeyPolicies = _projectKeyPolicies.GetKeyPoliciesForUserAssignment(userAssignment.ProjectId, userAssignment.AssigneeUsername);
        var resourceKeyPolicies = _resourceKeyPolicies.GetKeyPoliciesForUserAssignment(userAssignment.ProjectId, userAssignment.ResourceFullName, userAssignment.AssigneeUsername);

        var keyPolicies = new List<KeyPolicy>(projectKeyPolicies.Count + resourceKeyPolicies.Count);
        keyPolicies.AddRange(projectKeyPolicies);
        keyPolicies.AddRange(resourceKeyPolicies);

        await _projectsTableService.DeleteTableEntitiesAsync(keyPolicies);
    }

    public async Task StoreProjectAsync(Project project)
    {
        var tableEntities = new List<ITableEntity>();

        var byIdKeyPolicy = _projectKeyPolicies.GetKeyPolicyForGetById(project.Id);
        tableEntities.Add(ProjectTableEntity.FromProject(project, byIdKeyPolicy.PartitionKey, byIdKeyPolicy.RowKey.Value));

        var byStateKeyPolicies = _projectKeyPolicies.GetKeyPoliciesByState(project.State);
        tableEntities.AddRange(byStateKeyPolicies.Select(getByStateKeyPolicy => OrderedProjectTableEntity.FromProject(project, getByStateKeyPolicy.PartitionKey, getByStateKeyPolicy.RowKey.Value)).ToList());

        await _projectsTableService.StoreTableEntitiesAsync(tableEntities);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var projectTableEntity = await GetProjectTableEntityByIdAsync(project.Id);

        if (projectTableEntity.IsNone)
        {
            throw new InvalidOperationException($"Cannot update project with ID \"{project.Id}\" - no such project is stored");
        }

        foreach (var propInfo in typeof(ProjectBase).GetProperties(BindingFlags.Public| BindingFlags.Instance))
        {
            propInfo.SetValue(projectTableEntity.Value, propInfo.GetValue(project));
        }

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity.Value);
    }

    public async Task<Option<ProjectTableEntity>> UpdateProjectStateAsync(Guid projectId, string newState)
    {
        var optProjectTableEntity = await GetProjectTableEntityByIdAsync(projectId);

        if (optProjectTableEntity.IsNone)
        {
            throw new ArgumentException($"Cannot update project state: Project with ID \"{projectId}\" could not be found.", nameof(projectId));
        }

        var projectTableEntity = optProjectTableEntity.Value;
        var previousState = projectTableEntity.State;

        if (previousState.Equals(newState))
        {
            return Option<ProjectTableEntity>.None();
        }

        projectTableEntity.State = newState;

        await _projectsTableService.UpdateTableEntitiesAsync(projectTableEntity);

        // Delete the previous-state entities
        // - This entity represent an instance of OrderedProjectTableEntity.
        // - We can only obtain these by PartitionKey and Id. (The RowKey does not speficy any project-specific information.)
        // - Consequently we must obtain the entities in order to know PartitionKey and RowKey, then delete them.
        var previousStateTableEntities = await GetProjectTableEntityByStateAndIdAsync(previousState, projectId);
        foreach (var previousStateTableEntity in previousStateTableEntities)
        {
            await _projectsTableService.DeleteTableEntityAsync(previousStateTableEntity.PartitionKey, previousStateTableEntity.RowKey);
        }

        // Add the new-state entities.
        // These entities represents instances of OrderedProjectTableEntity.
        var newKeyPolicies = _projectKeyPolicies.GetKeyPoliciesByState(newState);
        foreach (var newKeyPolicy in newKeyPolicies)
        {
            projectTableEntity.PartitionKey = newKeyPolicy.PartitionKey;
            projectTableEntity.RowKey = newKeyPolicy.RowKey.Value;

            var orderedProjectTableEntity = OrderedProjectTableEntity.FromProjectTableEntity(projectTableEntity);

            await _projectsTableService.StoreTableEntitiesAsync(orderedProjectTableEntity);
        }

        return Option<ProjectTableEntity>.Some(projectTableEntity);
    }

    private async Task<Option<ProjectTableEntity>> GetProjectTableEntityByIdAsync(Guid projectId)
    {
        const int maxResultsPerPage = 1;
        var propNamesToSelect = GetPropertyNames<ProjectTableEntity>();

        var keyPolicyForGet = _projectKeyPolicies.GetKeyPolicyForGetById(projectId);
        var keyPolicyFilter = _keyPolicyFilterGeneration.ToFilter(keyPolicyForGet);

        var tableClient = _projectsTableService.TableClient.Value;

        var results = tableClient.QueryAsync<ProjectTableEntity>(keyPolicyFilter,
                                                                 maxPerPage: maxResultsPerPage,
                                                                 select: propNamesToSelect,
                                                                 cancellationToken: CancellationToken.None);

        await foreach (var result in results)
        {
            return Option<ProjectTableEntity>.Some(result);
        }

        return Option<ProjectTableEntity>.None();
    }

    private async Task<IReadOnlyCollection<OrderedProjectTableEntity>> GetProjectTableEntityByStateAndIdAsync(string state, Guid projectId)
    {
        var propNamesToSelect = GetPropertyNames<OrderedProjectTableEntity>();

        var keyPoliciesForGetByState = _projectKeyPolicies.GetKeyPoliciesByState(state);
        var keyPolicyFilters = keyPoliciesForGetByState.Select(keyPolicy => $"PartitionKey eq '{keyPolicy.PartitionKey}' and Id eq guid'{projectId}'");
        var concatenatedFilters = keyPolicyFilters.Aggregate((a, b) => $"({a}) or ({b})");
        var maxResultsPerPage = keyPoliciesForGetByState.Count;

        var tableClient = _projectsTableService.TableClient.Value;

        var results = tableClient.QueryAsync<OrderedProjectTableEntity>(concatenatedFilters,
                                                                        maxPerPage: maxResultsPerPage,
                                                                        select: propNamesToSelect,
                                                                        cancellationToken: CancellationToken.None);

        var orderedProjectTableEntities = new List<OrderedProjectTableEntity>(keyPoliciesForGetByState.Count);

        await foreach (var result in results)
        {
            orderedProjectTableEntities.Add(result);
        }

        return orderedProjectTableEntities;
    }

    private static IReadOnlyCollection<string> GetPropertyNames<TType>()
    {
        return typeof(TType).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(propInfo => propInfo.Name)
                            .ToList();
    }
}
