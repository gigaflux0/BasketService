using Application.Ports;

namespace PricesApi;

public class FakePriceApi : IPriceApi
{
    public Task<PriceResult> GetPriceAsync(string productId, CancellationToken ct)
    {
        if (productId == "notDiscounted")
        {
            return Task.FromResult(new PriceResult(
                ProductId: productId,
                BasePrice: 10.00m,
                DiscountedPrice: 10.00m
            ));
        }

        var result = new PriceResult(
            ProductId: productId,
            BasePrice: 10.00m,
            DiscountedPrice: 8.00m
        );

        return Task.FromResult(result);
    }
}