using Microsoft.Azure.Cosmos;
using ProjectionWorker.Ports;

namespace CosmosDb.EventStore;

public class CosmosEventStreamSubscription : IEventStreamSubscription
{
    private readonly Func<CosmosClient> _clientFactory;
    private CosmosClient? _client;

    public CosmosEventStreamSubscription(Func<CosmosClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <summary>
    ///     This is started up by a hosted service, and fed a func to call when an event is added to the store.
    /// </summary>
    public async Task StartAsync(
        Func<StreamEventNotification, CancellationToken, Task> onEvent,
        CancellationToken ct)
    {
        // The worker can be too eager to connect when launching DB and Api at the same time with docker compose
        // Retry until Cosmos is reachable
        for (var i = 0; i < 30; i++)
        {
            try
            {
                _client = _clientFactory();

                var dbResponse = await _client.CreateDatabaseIfNotExistsAsync("BasketDb", cancellationToken: ct);
                var database = dbResponse.Database;

                var eventsContainerResponse = await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties("basketEvents", "/StreamId"), cancellationToken: ct);

                var eventsContainer = eventsContainerResponse.Container;

                // If we got here, Cosmos is ready — break out of the loop
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cosmos not ready yet: {ex.Message}");
                await Task.Delay(1000, ct);
            }
        }

        if (_client is null)
            throw new InvalidOperationException("CosmosClient could not be initialized after retries.");

        var iterator = _client
            .GetDatabase("BasketDb")
            .GetContainer("basketEvents")
            .GetChangeFeedIterator<EventDocument>(
                ChangeFeedStartFrom.Now(),
                ChangeFeedMode.Incremental);

        while (!ct.IsCancellationRequested)
        {
            var response = await iterator.ReadNextAsync(ct);

            foreach (var doc in response)
            {
                // We only really care an event happened at all for now, that's enough information for
                // the projection builder to go rehydrate the projection with.
                var notification = new StreamEventNotification(
                    doc.StreamId
                );

                await onEvent(notification, ct);
            }

            // Determines our polling frequency
            await Task.Delay(100, ct);
        }
    }
}