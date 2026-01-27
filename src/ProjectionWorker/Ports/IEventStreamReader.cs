using Domain.Events;

namespace ProjectionWorker.Ports;

public interface IEventStreamReader
{
    Task<IReadOnlyList<IBasketEvent>> LoadEventsAsync(string streamId, CancellationToken ct = default);
}
