using System.Net;
using CosmosDb.ReadModel;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using ProjectionWorker.Models;

namespace CosmosDb.UnitTests.ReadModel;

[TestFixture]
public class CosmosBasketReadModelRepositoryTests
{
    private CosmosClient _client = null!;
    private Database _database = null!;
    private Container _container = null!;
    private CosmosBasketReadModelRepository _repo = null!;

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

        _client.CreateDatabaseIfNotExistsAsync(
                "BasketDb",
                Arg.Any<int?>(),
                Arg.Any<RequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbResponse));

        _database.CreateContainerIfNotExistsAsync(
                Arg.Any<ContainerProperties>())
            .Returns(Task.FromResult(containerResponse));

        _database.CreateContainerIfNotExistsAsync(
                Arg.Any<ContainerProperties>(),
                Arg.Any<int?>(),
                Arg.Any<RequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(containerResponse));

        _repo = new CosmosBasketReadModelRepository(() => _client);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task GetAsync_WhenItemExists_ReturnsMappedReadModel()
    {
        var basketId = "basket-123";

        var projection = new BasketProjection
        {
            Id = basketId,
            DiscountCode = "SAVE10",
            Items = new List<BasketItemProjection>
            {
                new() { ProductId = "prod-1", Quantity = 2 },
                new() { ProductId = "prod-2", Quantity = 1 }
            },
            ShippingCosts = new Dictionary<string, decimal>
            {
                { "UK", 5m },
                { "EU", 10m }
            }
        };

        var response = Substitute.For<ItemResponse<BasketProjection>>();
        response.Resource.Returns(projection);

        _container.ReadItemAsync<BasketProjection>(
                basketId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var result = await _repo.GetAsync(basketId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.BasketId.Should().Be(basketId);
        result.DiscountCode.Should().Be("SAVE10");
        result.Items.Should().HaveCount(2);
        result.ShippingCosts.Should().ContainKey("UK").WhoseValue.Should().Be(5m);
    }

    [Test]
    public async Task GetAsync_WhenItemDoesNotExist_ReturnsNull()
    {
        var basketId = "missing-basket";

        var notFound = new CosmosException(
            message: "Not found",
            statusCode: HttpStatusCode.NotFound,
            subStatusCode: 0,
            activityId: string.Empty,
            requestCharge: 0);

        _container.ReadItemAsync<BasketProjection>(
                basketId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ItemResponse<BasketProjection>>(notFound));

        var result = await _repo.GetAsync(basketId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_EnsuresDatabaseAndContainerAreCreated()
    {
        var basketId = "basket-123";

        var projection = new BasketProjection
        {
            Id = basketId,
            Items = new List<BasketItemProjection>(),
            ShippingCosts = new Dictionary<string, decimal>()
        };

        var response = Substitute.For<ItemResponse<BasketProjection>>();
        response.Resource.Returns(projection);

        _container.ReadItemAsync<BasketProjection>(
                basketId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        await _repo.GetAsync(basketId, CancellationToken.None);

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
    }
}