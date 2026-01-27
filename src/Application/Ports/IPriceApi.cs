namespace Application.Ports;

public interface IPriceApi
{
    Task<PriceResult> GetPriceAsync(string productId, CancellationToken ct);
}

public record PriceResult(
    string ProductId,
    decimal BasePrice,
    decimal DiscountedPrice
);