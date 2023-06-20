﻿using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;

namespace Opsi.Services.Specs;

[TestClass]
public class ProjectsTableServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Project _project;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ProjectsTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _project = new Project { Id = Guid.NewGuid() };
        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _tableService.GetTableClient()).Returns(_tableClient);
        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new ProjectsTableService(_tableServiceFactory);
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenMatchingProjectFound_ReturnsProject()
    {
        var projectsResult = new List<Project> { _project };
        var page = Page<Project>.FromValues(projectsResult,
                                            continuationToken: null,
                                            response: A.Fake<Response>());
        var pages = AsyncPageable<Project>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<Project>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(_project.Id.ToString())),
                                                        A<int?>._,
                                                        A<IEnumerable<string>>._,
                                                        A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_project.Id);

        result.Should()
            .NotBeNull()
            .And.Match<Project>(m => m.Id.ToString().Equals(_project.Id.ToString()));
    }

    [TestMethod]
    public async Task GetProjectByIdAsync_WhenNoMatchingProjectFound_ReturnsNull()
    {
        var newProject = new Project { Id = Guid.NewGuid() };
        var projectsResult = new List<Project> { newProject };
        var page = Page<Project>.FromValues(projectsResult,
                                            continuationToken: null,
                                            response: A.Fake<Response>());
        var pages = AsyncPageable<Project>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<Project>(A<string>.That.Matches(filter => filter.Contains(nameof(Project.Id)) && filter.Contains(newProject.Id.ToString())),
                                                        A<int?>._,
                                                        A<IEnumerable<string>>._,
                                                        A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetProjectByIdAsync(_project.Id);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectToTableService()
    {
        await _testee.StoreProjectAsync(_project);

        A.CallTo(() => _tableService.StoreTableEntityAsync(_project)).MustHaveHappenedOnceExactly();
    }
}
