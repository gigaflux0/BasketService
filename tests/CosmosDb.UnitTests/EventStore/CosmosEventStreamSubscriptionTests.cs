using CosmosDb.EventStore;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using ProjectionWorker.Ports;

namespace CosmosDb.UnitTests.EventStore;

[TestFixture]
public class CosmosEventStreamSubscriptionTests
{
    private CosmosClient _client = null!;
    private Database _database = null!;
    private Container _container = null!;
    private FeedIterator<EventDocument> _iterator = null!;
    private CosmosEventStreamSubscription _subscription = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<CosmosClient>();
        _database = Substitute.For<Database>();
        _container = Substitute.For<Container>();
        _iterator = Substitute.For<FeedIterator<EventDocument>>();

        var dbResponse = Substitute.For<DatabaseResponse>();
        dbResponse.Database.Returns(_database);

        var containerResponse = Substitute.For<ContainerResponse>();
        containerResponse.Container.Returns(_container);

        _client.CreateDatabaseIfNotExistsAsync(
                "BasketDb",
                Arg.Any<int?>(),
                Arg.Any<RequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbResponse));

        _database.CreateContainerIfNotExistsAsync(
                Arg.Any<ContainerProperties>(),
                Arg.Any<int?>(),
                Arg.Any<RequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(containerResponse));

        _client.GetDatabase("BasketDb").Returns(_database);
        _database.GetContainer("basketEvents").Returns(_container);

        _container.GetChangeFeedIterator<EventDocument>(
                Arg.Any<ChangeFeedStartFrom>(),
                ChangeFeedMode.Incremental)
            .Returns(_iterator);

        _subscription = new CosmosEventStreamSubscription(() => _client);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _iterator?.Dispose();
    }

    [Test]
    public async Task StartAsync_WhenClientFactoryAlwaysThrows_ThrowsInvalidOperationException()
    {
        var failingSubscription = new CosmosEventStreamSubscription(() => throw new Exception("factory failed"));

        var cts = new CancellationTokenSource();

        await failingSubscription
            .Invoking(s => s.StartAsync((_, _) => Task.CompletedTask, cts.Token))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task StartAsync_WhenCosmosInitializes_UsesChangeFeedIterator()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var response = Substitute.For<FeedResponse<EventDocument>>();
        using var enumerator = response.GetEnumerator();
        using var docEnumerator = Enumerable.Empty<EventDocument>().GetEnumerator();
        enumerator.Returns(docEnumerator);

        _iterator.ReadNextAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<FeedResponse<EventDocument>>(new TaskCanceledException()),
                _ => Task.FromResult(response)
            );

        Func<StreamEventNotification, CancellationToken, Task> onEvent =
            (_, _) => Task.CompletedTask;

        await _subscription
            .Invoking(s => s.StartAsync(onEvent, cts.Token))
            .Should().ThrowAsync<TaskCanceledException>();

        await _iterator.Received().ReadNextAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenEventsArrive_InvokesCallbackForEach()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var docs = new[]
        {
            new EventDocument { StreamId = "basket-1" },
            new EventDocument { StreamId = "basket-2" }
        };

        var response = Substitute.For<FeedResponse<EventDocument>>();
        using var enumerator = response.GetEnumerator();
        using var docEnumerator = docs.AsEnumerable().GetEnumerator();
        enumerator.Returns(docEnumerator);

        _iterator.ReadNextAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult(response),
                _ => Task.FromException<FeedResponse<EventDocument>>(new TaskCanceledException())
            );

        var received = new List<string>();

        Func<StreamEventNotification, CancellationToken, Task> onEvent =
            (n, _) =>
            {
                received.Add(n.StreamId);
                return Task.CompletedTask;
            };

        await _subscription
            .Invoking(s => s.StartAsync(onEvent, cts.Token))
            .Should().ThrowAsync<TaskCanceledException>();

        received.Should().BeEquivalentTo("basket-1", "basket-2");
    }
}