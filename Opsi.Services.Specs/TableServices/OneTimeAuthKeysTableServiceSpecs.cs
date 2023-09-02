using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class OneTimeAuthKeysTableServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private OneTimeAuthKeyEntity _entity;
    private string _username;
    private string _key;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private OneTimeAuthKeysTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _key = Guid.NewGuid().ToString();
        _username = Guid.NewGuid().ToString();
        _entity = new OneTimeAuthKeyEntity(_username, _key);

        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _tableService.TableClient.Value).Returns(_tableClient);
        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new OneTimeAuthKeysTableService(_tableServiceFactory);
    }

    [TestMethod]
    public async Task DeleteKeyAsync_PassesProjectIdAndKeyToTableService()
    {
        await _testee.DeleteKeyAsync(_username, _key);

        A.CallTo(() => _tableClient.DeleteEntityAsync(A<string>.That.Matches(s => s.Equals(_username)),
                                                      _key,
                                                      A<ETag>._,
                                                      CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetKeyAsync_WhenMatchingEntityFound_ReturnsKey()
    {
        var entitiesResult = new List<OneTimeAuthKeyEntity> { _entity };
        var page = Page<OneTimeAuthKeyEntity>.FromValues(entitiesResult,
                                                         continuationToken: null,
                                                         response: A.Fake<Response>());
        var pages = AsyncPageable<OneTimeAuthKeyEntity>.FromPages(new[] { page });
        A.CallTo(() => _tableClient.QueryAsync<OneTimeAuthKeyEntity>(A<string>.That.Matches(filter => filter.Contains(_username) && filter.Contains(_key)),
                                                                     A<int?>._,
                                                                     A<IEnumerable<string>>._,
                                                                     A<CancellationToken>._)).Returns(pages);

        var result = await _testee.AreDetailsValidAsync(_username, _key);

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetKeyAsync_WhenNoMatchingEntityFound_ReturnsNull()
    {
        var entitiesResult = new List<OneTimeAuthKeyEntity>(0);
        var page = Page<OneTimeAuthKeyEntity>.FromValues(entitiesResult,
                                                         continuationToken: null,
                                                         response: A.Fake<Response>());
        var pages = AsyncPageable<OneTimeAuthKeyEntity>.FromPages(new[] { page });
        A.CallTo(() => _tableClient.QueryAsync<OneTimeAuthKeyEntity>(A<string>.That.Matches(filter => filter.Contains(_username) && filter.Contains(_key)),
                                                                     A<int?>._,
                                                                     A<IEnumerable<string>>._,
                                                                     A<CancellationToken>._)).Returns(pages);

        var result = await _testee.AreDetailsValidAsync(_username, _key);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task StoreKeyAsync_PassesEntityToTableService()
    {
        await _testee.StoreKeyAsync(_entity);

        A.CallTo(() => _tableService.StoreTableEntitiesAsync(_entity)).MustHaveHappenedOnceExactly();
    }
}
