using System.Net;
using Application.Ports;
using Microsoft.Azure.Cosmos;
using ProjectionWorker.Models;

namespace CosmosDb.ReadModel;

public class CosmosBasketReadModelRepository : IBasketReadModelRepository
{
    private readonly CosmosClient _client;

    public CosmosBasketReadModelRepository(Func<CosmosClient> clientFactory)
    {
        _client = clientFactory();
    }

    public async Task<BasketReadModel?> GetAsync(string basketId, CancellationToken ct)
    {
        var container = await GetReadModelContainerAsync();

        try
        {
            var response = await container.ReadItemAsync<BasketProjection>(
                id: basketId,
                partitionKey: new PartitionKey(basketId),
                cancellationToken: ct);

            var p = response.Resource;

            return new BasketReadModel(
                BasketId: p.Id,
                DiscountCode: p.DiscountCode,
                Items: p.Items.Select(i => new BasketReadModelItem(i.ProductId, i.Quantity)).ToList(),
                ShippingCosts: new Dictionary<string, decimal>(p.ShippingCosts)
            );
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Container> GetReadModelContainerAsync()
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync("BasketDb");

        var props = new ContainerProperties(
            id: "basketReadModels",
            partitionKeyPath: "/id"
        );

        var containerResponse = await db.Database.CreateContainerIfNotExistsAsync(props);

        return containerResponse.Container;
    }

}
