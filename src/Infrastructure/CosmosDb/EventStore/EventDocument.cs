using Newtonsoft.Json;

namespace CosmosDb.EventStore;

public class EventDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    public string StreamId { get; init; } = null!;
    public string EventType { get; init; } = null!;
    public string Payload { get; init; } = null!;
    public int SequenceNumber { get; init; }
    public DateTime OccurredAt { get; set; }
}