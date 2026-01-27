using Application.Abstractions;
using Application.Ports;
using FluentResults;

namespace Application.Basket.Queries;

public sealed class GetBasketTotalsHandler : IQueryHandler<GetBasketTotalsQuery, BasketTotalsResult>
{
    const decimal VatRate = 0.2m;

    private readonly IBasketReadModelRepository _readModel;
    private readonly IPriceApi _priceApi;
    private readonly IDiscountCodesApi _discountApi;

    public GetBasketTotalsHandler(
        IBasketReadModelRepository readModel,
        IPriceApi priceApi,
        IDiscountCodesApi discountApi)
    {
        _readModel = readModel;
        _priceApi = priceApi;
        _discountApi = discountApi;
    }

    public async Task<Result<BasketTotalsResult>> Handle(
        GetBasketTotalsQuery query,
        CancellationToken ct)
    {
        try
        {
            var streamId = $"basket-{query.BasketId}";
            var basket = await _readModel.GetAsync(streamId, ct);
            if (basket is null)
                return Result.Fail("Basket not found.");

            var subtotalBeforeDiscounts = 0m;
            var itemDiscounts = 0m;
            var subtotalOfFullPriceItems = 0m;

            // Gather the item prices, including discounts
            foreach (var item in basket.Items)
            {
                var price = await _priceApi.GetPriceAsync(item.ProductId, ct);

                subtotalBeforeDiscounts += price.BasePrice * item.Quantity;
                itemDiscounts += (price.BasePrice - price.DiscountedPrice) * item.Quantity;
                if (price.BasePrice == price.DiscountedPrice)
                {
                    subtotalOfFullPriceItems += price.BasePrice * item.Quantity;
                }
            }

            // Figure out how much money the discount code saves if any
            var discountCodeAmount = 0m;
            if (!string.IsNullOrWhiteSpace(basket.DiscountCode))
            {
                var discount = await _discountApi.GetDiscountAsync(basket.DiscountCode, ct);
                if (discount is not null)
                {
                    discountCodeAmount = discount.Type switch
                    {
                        "percentage" => subtotalOfFullPriceItems * (discount.Value / 100m),
                        "fixed" => discount.Value,
                        _ => discountCodeAmount
                    };
                }
            }

            var subtotalAfterDiscounts = subtotalBeforeDiscounts - itemDiscounts - discountCodeAmount;

            // VAT is 20%, we apply it after shipping costs and item discounts
            var vatOnItems = subtotalAfterDiscounts * VatRate;

            var shippingCost = basket.ShippingCosts.GetValueOrDefault(query.Country, 0m);
            var shippingVat = shippingCost * VatRate;

            var totalWithoutVat = subtotalAfterDiscounts + shippingCost;
            var totalWithVat = totalWithoutVat + vatOnItems + shippingVat;

            var totalSavings = itemDiscounts + discountCodeAmount;

            var result = new BasketTotalsResult(
                SubtotalBeforeDiscounts: subtotalBeforeDiscounts,
                ItemDiscounts: itemDiscounts,
                DiscountCodeAmount: discountCodeAmount,
                SubtotalAfterDiscounts: subtotalAfterDiscounts,
                VatOnItems: vatOnItems,
                ShippingCost: shippingCost,
                ShippingVat: shippingVat,
                TotalWithoutVat: totalWithoutVat,
                TotalWithVat: totalWithVat,
                TotalSavings: totalSavings
            );

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError(ex));
        }
    }
}
