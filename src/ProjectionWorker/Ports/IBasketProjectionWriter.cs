using ProjectionWorker.Models;

namespace ProjectionWorker.Ports;

public interface IBasketProjectionWriter
{
    Task SaveAsync(BasketProjection projection, CancellationToken ct = default);
}
