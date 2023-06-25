using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Services.InternalTypes;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class WebhookTableServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private WebhookTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _tableClient = A.Fake<TableClient>();
        _tableService = A.Fake<ITableService>();
        _tableServiceFactory = A.Fake<ITableServiceFactory>();

        A.CallTo(() => _tableService.GetTableClient()).Returns(_tableClient);
        A.CallTo(() => _tableServiceFactory.Create(A<string>._)).Returns(_tableService);

        _testee = new WebhookTableService(_tableServiceFactory);
    }

    [TestMethod]
    public async Task GetUndeliveredAsync_WhenResultCountIsNonZero_ReturnsResultsOfTableQuery()
    {
        var callbackResults = GetInternalCallbackMessages().Take(2).ToList();
        var page = Page<InternalCallbackMessage>.FromValues(callbackResults,
                                                            continuationToken: null,
                                                            response: A.Fake<Response>());
        var pages = AsyncPageable<InternalCallbackMessage>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<InternalCallbackMessage>(A<string>.That.Matches(filter => filter.Contains(nameof(InternalCallbackMessage.IsDelivered)) && filter.Contains(nameof(InternalCallbackMessage.FailureCount))),
                                                                        A<int?>._,
                                                                        A<IEnumerable<string>>._,
                                                                        A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetUndeliveredAsync();

        result.Should()
              .NotBeNull()
              .And.HaveCount(callbackResults.Count);
    }

    [TestMethod]
    public async Task GetUndeliveredAsync_WhenResultCountIsZero_ReturnsEmptyCollection()
    {
        var callbackResults = GetInternalCallbackMessages().Take(0).ToList();
        var page = Page<InternalCallbackMessage>.FromValues(callbackResults,
                                                            continuationToken: null,
                                                            response: A.Fake<Response>());
        var pages = AsyncPageable<InternalCallbackMessage>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<InternalCallbackMessage>(A<string>.That.Matches(filter => filter.Contains(nameof(InternalCallbackMessage.IsDelivered)) && filter.Contains(nameof(InternalCallbackMessage.FailureCount))),
                                                                        A<int?>._,
                                                                        A<IEnumerable<string>>._,
                                                                        A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetUndeliveredAsync();

        result.Should()
              .NotBeNull()
              .And.BeEmpty();
    }

    [TestMethod]
    public async Task StoreProjectAsync_PassesProjectToTableService()
    {
        var internalCallbackMessage = GetInternalCallbackMessages().Take(1).Single();

        await _testee.StoreAsync(internalCallbackMessage);

        A.CallTo(() => _tableService.StoreTableEntityAsync(internalCallbackMessage)).MustHaveHappenedOnceExactly();
    }

    private static IEnumerable<InternalCallbackMessage> GetInternalCallbackMessages()
    {
        var statusIndex = 0;

        while (true)
        {
            yield return new InternalCallbackMessage
            {
                Status = statusIndex++.ToString()
            };
        }
    }
}
