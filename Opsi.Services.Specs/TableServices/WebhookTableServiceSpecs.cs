using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using FakeItEasy;
using FluentAssertions;
using Opsi.AzureStorage;
using Opsi.AzureStorage.TableEntities;
using Opsi.Pocos;
using Opsi.Services.TableServices;

namespace Opsi.Services.Specs.TableServices;

[TestClass]
public class WebhookTableServiceSpecs
{
    private const string _customProp1Name = nameof(_customProp1Name);
    private const string _customProp1Value = nameof(_customProp1Value);
    private const string _customProp2Name = nameof(_customProp2Name);
    private const int _customProp2Value = 2;
    private const string RemoteUri = "https://a.test.url";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private string _serialisedCustomProps;
    private TableClient _tableClient;
    private ITableService _tableService;
    private ITableServiceFactory _tableServiceFactory;
    private ConsumerWebhookSpecification _webhookSpec;
    private WebhookTableService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _webhookSpec = new ConsumerWebhookSpecification
        {
            CustomProps = new Dictionary<string, object>
            {
                {_customProp1Name, _customProp1Value },
                {_customProp2Name, _customProp2Value }
            },
            Uri = RemoteUri
        };

        _serialisedCustomProps = JsonSerializer.Serialize(_webhookSpec.CustomProps);
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
        var Results = GetInternalWebhookMessageTableEntities().Take(2).ToList();
        var page = Page<InternalWebhookMessageTableEntity>.FromValues(Results,
                                                           continuationToken: null,
                                                           response: A.Fake<Response>());
        var pages = AsyncPageable<InternalWebhookMessageTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<InternalWebhookMessageTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(InternalWebhookMessage.IsDelivered)) && filter.Contains(nameof(InternalWebhookMessage.FailureCount))),
                                                                                  A<int?>._,
                                                                                  A<IEnumerable<string>>._,
                                                                                  A<CancellationToken>._)).Returns(pages);

        var result = await _testee.GetUndeliveredAsync();

        result.Should()
              .NotBeNull()
              .And.HaveCount(Results.Count);
    }

    [TestMethod]
    public async Task GetUndeliveredAsync_WhenResultCountIsZero_ReturnsEmptyCollection()
    {
        var Results = GetInternalWebhookMessageTableEntities().Take(0).ToList();
        var page = Page<InternalWebhookMessageTableEntity>.FromValues(Results,
                                                           continuationToken: null,
                                                           response: A.Fake<Response>());
        var pages = AsyncPageable<InternalWebhookMessageTableEntity>.FromPages(new[] { page });

        A.CallTo(() => _tableClient.QueryAsync<InternalWebhookMessageTableEntity>(A<string>.That.Matches(filter => filter.Contains(nameof(InternalWebhookMessageTableEntity.IsDelivered)) && filter.Contains(nameof(InternalWebhookMessageTableEntity.FailureCount))),
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
        var internalWebhookMessage= GetInternalWebhookMessages().Take(1).Single();
        internalWebhookMessage.FailureCount = 0;

        await _testee.StoreAsync(internalWebhookMessage);

        A.CallTo(() => _tableService.StoreTableEntitiesAsync(A<InternalWebhookMessageTableEntity>.That.Matches(entity => entity.Id.Equals(internalWebhookMessage.Id)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task UpdateProjectAsync_PassesProjectToTableService()
    {
        var internalWebhookMessage = GetInternalWebhookMessages().Take(1).Single();
        internalWebhookMessage.FailureCount = 9;

        await _testee.StoreAsync(internalWebhookMessage);

        A.CallTo(() => _tableService.UpdateTableEntitiesAsync(A<InternalWebhookMessageTableEntity>.That.Matches(entity => entity.Id.Equals(internalWebhookMessage.Id)))).MustHaveHappenedOnceExactly();
    }

    private IEnumerable<InternalWebhookMessage> GetInternalWebhookMessages()
    {
        var statusIndex = 0;

        while (true)
        {
            yield return new InternalWebhookMessage
            {
                Event = statusIndex++.ToString(),
                FailureCount = 9,
                Id = Guid.NewGuid(),
                Level = statusIndex.ToString(),
                IsDelivered = true,
                Name = statusIndex.ToString(),
                OccurredOn = DateTime.Now,
                ProjectId = Guid.NewGuid(),
                Username = "user@test.com",
                WebhookSpecification = _webhookSpec
            };
        }
    }

    private IEnumerable<InternalWebhookMessageTableEntity> GetInternalWebhookMessageTableEntities()
    {
        var statusIndex = 0;

        while (true)
        {
            yield return new InternalWebhookMessageTableEntity
            {
                Event = statusIndex++.ToString(),
                FailureCount = 9,
                Id = Guid.NewGuid(),
                IsDelivered = true,
                Level = statusIndex.ToString(),
                Name = statusIndex.ToString(),
                OccurredOn = DateTime.Now,
                ProjectId = Guid.NewGuid(),
                Username = "user@test.com",
                WebhookUri = _webhookSpec.Uri!,
                SerialisedWebhookCustomProps = _serialisedCustomProps
            };
        }
    }
}
