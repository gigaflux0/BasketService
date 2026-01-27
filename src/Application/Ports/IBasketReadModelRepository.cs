using Application.Basket.Queries;

namespace Application.Ports;

public interface IBasketReadModelRepository
{
    Task<BasketReadModel?> GetAsync(string basketId, CancellationToken ct);
}
