using System.Text.Json;
using CosmosDb.EventStore;
using Domain.Events;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using FluentAssertions;

namespace CosmosDb.UnitTests.EventStore;

[TestFixture]
public class CosmosBasketRepositoryTests
{
    private CosmosClient _client = null!;
    private Database _database = null!;
    private Container _container = null!;
    private CosmosBasketRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<CosmosClient>();
        _database = Substitute.For<Database>();
        _container = Substitute.For<Container>();

        var dbResponse = Substitute.For<DatabaseResponse>();
        dbResponse.Database.Returns(_database);

        var containerResponse = Substitute.For<ContainerResponse>();
        containerResponse.Container.Returns(_container);

        _client.CreateDatabaseIfNotExistsAsync("BasketDb")
            .Returns(Task.FromResult(dbResponse));

        _database.CreateContainerIfNotExistsAsync(Arg.Any<ContainerProperties>())
            .Returns(Task.FromResult(containerResponse));


        _repo = new CosmosBasketRepository(() => _client);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task LoadEventsAsync_ReturnsDeserializedEvents()
    {
        var streamId = "basket-123";

        var docs = new[]
        {
            new EventDocument
            {
                EventType = typeof(BasketCreated).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new BasketCreated("123", 1, DateTime.UtcNow)),
                SequenceNumber = 1
            },
            new EventDocument
            {
                EventType = typeof(BasketItemAdded).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new BasketItemAdded("prod-1", 2, 2, DateTime.UtcNow)),
                SequenceNumber = 2
            }
        };

        var iterator = Substitute.For<FeedIterator<EventDocument>>();
        iterator.HasMoreResults.Returns(true, false);

        var response = Substitute.For<FeedResponse<EventDocument>>();
        response.GetEnumerator().Returns(docs.AsEnumerable().GetEnumerator());

        iterator.ReadNextAsync(Arg.Any<CancellationToken>())
            .Returns(response);

        _container.GetItemQueryIterator<EventDocument>(Arg.Any<QueryDefinition>())
            .Returns(iterator);

        var result = await _repo.LoadEventsAsync(streamId);

        result.Should().HaveCount(2);
        result[0].Should().BeOfType<BasketCreated>();
        result[1].Should().BeOfType<BasketItemAdded>();
    }

    [Test]
    public async Task SaveEventsAsync_WhenConcurrencyMismatch_Throws()
    {
        var streamId = "basket-123";

        _container.GetItemQueryIterator<EventDocument>(Arg.Any<QueryDefinition>())
            .Returns(Substitute.For<FeedIterator<EventDocument>>());

        await _repo.Invoking(r => r.SaveEventsAsync(streamId, new List<IBasketEvent>(), 5))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task SaveEventsAsync_WritesEventsWithIncrementingSequenceNumbers()
    {
        var streamId = "basket-123";

        var existingDocs = new[]
        {
            new EventDocument
            {
                EventType = typeof(BasketCreated).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new BasketCreated("123", 1, DateTime.UtcNow)),
                SequenceNumber = 1
            }
        };

        var iterator = Substitute.For<FeedIterator<EventDocument>>();
        iterator.HasMoreResults.Returns(true, false);

        var response = Substitute.For<FeedResponse<EventDocument>>();
        response.GetEnumerator().Returns(existingDocs.AsEnumerable().GetEnumerator());

        iterator.ReadNextAsync(Arg.Any<CancellationToken>())
            .Returns(response);

        _container.GetItemQueryIterator<EventDocument>(Arg.Any<QueryDefinition>())
            .Returns(iterator);

        var newEvents = new List<IBasketEvent>
        {
            new BasketItemAdded("prod-1", 2, 0, DateTime.UtcNow),
            new BasketItemAdded("prod-2", 1, 0, DateTime.UtcNow)
        };

        await _repo.SaveEventsAsync(streamId, newEvents, 1);

        await _container.Received(1).CreateItemAsync(
            Arg.Is<EventDocument>(d =>
                d.StreamId == streamId &&
                d.SequenceNumber == 2 &&
                d.Id == $"{streamId}-2"
            ),
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());

        await _container.Received(1).CreateItemAsync(
            Arg.Is<EventDocument>(d =>
                d.StreamId == streamId &&
                d.SequenceNumber == 3 &&
                d.Id == $"{streamId}-3"
            ),
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());
    }
}