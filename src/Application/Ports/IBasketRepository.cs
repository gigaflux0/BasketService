using Domain.Events;

namespace Application.Ports;

public interface IBasketRepository
{
    Task<IReadOnlyList<IBasketEvent>> LoadEventsAsync(string streamId, CancellationToken ct = default);

    Task SaveEventsAsync(
        string streamId,
        IReadOnlyList<IBasketEvent> events,
        int expectedEventCountBeforeSaving,
        CancellationToken ct = default);
}
