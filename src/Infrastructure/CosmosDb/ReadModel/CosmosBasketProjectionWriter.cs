using Microsoft.Azure.Cosmos;
using ProjectionWorker.Ports;
using ProjectionWorker.Models;

namespace CosmosDb.ReadModel;

public class CosmosBasketProjectionWriter : IBasketProjectionWriter
{
    private readonly Func<CosmosClient> _clientFactory;

    public CosmosBasketProjectionWriter(Func<CosmosClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task SaveAsync(BasketProjection projection, CancellationToken ct)
    {
        var client = _clientFactory();

        var db = await client.CreateDatabaseIfNotExistsAsync("BasketDb", cancellationToken: ct);

        var containerResponse = await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties("basketReadModels", "/id"),
            cancellationToken: ct);

        var container = containerResponse.Container;

        await container.UpsertItemAsync(projection, cancellationToken: ct);
    }
}