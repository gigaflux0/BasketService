using CosmosDb.ReadModel;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using ProjectionWorker.Models;

namespace CosmosDb.UnitTests.ReadModel;

[TestFixture]
public class CosmosBasketProjectionWriterTests
{
    private CosmosClient _client = null!;
    private Database _database = null!;
    private Container _container = null!;
    private CosmosBasketProjectionWriter _writer = null!;

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

        _writer = new CosmosBasketProjectionWriter(() => _client);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task SaveAsync_CreatesDatabaseAndContainer_ThenUpsertsProjection()
    {
        var projection = new BasketProjection
        {
            Id = "basket-123",
            DiscountCode = "SAVE10",
            Items = new List<BasketItemProjection>(),
            ShippingCosts = new Dictionary<string, decimal> { { "UK", 5m } }
        };

        await _writer.SaveAsync(projection, CancellationToken.None);

        await _client.Received(1).CreateDatabaseIfNotExistsAsync(
            "BasketDb",
            Arg.Any<int?>(),
            Arg.Any<RequestOptions?>(),
            Arg.Any<CancellationToken>());

        await _database.Received(1).CreateContainerIfNotExistsAsync(
            Arg.Is<ContainerProperties>(p =>
                p.Id == "basketReadModels" &&
                p.PartitionKeyPath == "/id"),
            Arg.Any<int?>(),
            Arg.Any<RequestOptions?>(),
            Arg.Any<CancellationToken>());

        await _container.Received(1).UpsertItemAsync(
            projection,
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveAsync_WhenUpsertFails_Throws()
    {
        var projection = new BasketProjection
        {
            Id = "basket-999",
            Items = new List<BasketItemProjection>(),
            ShippingCosts = new Dictionary<string, decimal>()
        };

        _container.UpsertItemAsync(
                projection,
                Arg.Any<PartitionKey?>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ItemResponse<BasketProjection>>(
                new CosmosException(
                    "fail",
                    System.Net.HttpStatusCode.BadRequest,
                    0,
                    string.Empty,
                    0
                )));

        await _writer.Invoking(w => w.SaveAsync(projection, CancellationToken.None))
            .Should().ThrowAsync<CosmosException>();
    }
}