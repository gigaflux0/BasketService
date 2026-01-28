using System.Text.Json;
using Application.Ports;
using Domain.Events;
using Microsoft.Azure.Cosmos;
using ProjectionWorker.Ports;

namespace CosmosDb.EventStore;

public class CosmosBasketRepository : IBasketRepository, IEventStreamReader
{
    private readonly Func<CosmosClient> _clientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CosmosBasketRepository(Func<CosmosClient> clientFactory)
    {
        _clientFactory = clientFactory;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private CosmosClient Client => field ??= _clientFactory();

    public async Task<IReadOnlyList<IBasketEvent>> LoadEventsAsync(
        string streamId,
        CancellationToken ct = default)
    {
        var basketEventsContainer = await GetBasketEventsContainer();

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.StreamId = @streamId ORDER BY c.SequenceNumber ASC")
            .WithParameter("@streamId", streamId);

        var iterator = basketEventsContainer.GetItemQueryIterator<EventDocument>(query);

        var events = new List<IBasketEvent>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);

            foreach (var doc in response)
            {
                var type = Type.GetType(doc.EventType, throwOnError: true)!;

                var @event = (IBasketEvent)JsonSerializer.Deserialize(
                    doc.Payload,
                    type,
                    _jsonOptions
                )!;

                events.Add(@event);
            }
        }

        return events;
    }

    public async Task SaveEventsAsync(
        string streamId,
        IReadOnlyList<IBasketEvent> events,
        int expectedEventCountBeforeSaving,
        CancellationToken ct = default)
    {
        // We technically don't need to do this as the uniqueness constraint in the DB is the ultimate optimistic
        // concurrency check, but this could save us bothering the DB to produce an error
        var existing = await LoadEventsAsync(streamId, ct);
        if (existing.Count != expectedEventCountBeforeSaving)
            throw new InvalidOperationException("Concurrency conflict on event stream.");

        var sequenceNumber = expectedEventCountBeforeSaving;

        var basketEventsContainer = await GetBasketEventsContainer();

        foreach (var e in events)
        {
            sequenceNumber++;

            var doc = new EventDocument
            {
                Id = $"{streamId}-{sequenceNumber}",
                StreamId = streamId,
                EventType = e.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(e, e.GetType(), _jsonOptions),
                SequenceNumber = sequenceNumber,
                OccurredAt = e.OccurredAt
            };

            //     It's not impossible for something to fail here, say 2 events are lined up to save,
            // the first goes through but the second fails. This is a complicated irl scenario
            // best left to the event producers themselves to consider.
            //     As concurrency is optimistic we would also ideally try catch for the concurrency conflict exception
            // and treat this as an expected scenario in the system, propagating it all the way back to the initial
            // api and attempting the whole call again a few times.
            await basketEventsContainer.CreateItemAsync(doc, cancellationToken: ct);
        }
    }

    private async Task<Container> GetBasketEventsContainer()
    {
        Database database = await Client.CreateDatabaseIfNotExistsAsync(id: "BasketDb");
        var containerProperties = new ContainerProperties("basketEvents", "/StreamId");
        return await database.CreateContainerIfNotExistsAsync(containerProperties);
    }
}
