namespace Application.Basket.Queries;

public sealed record GetBasketTotalsQuery(
    string BasketId,
    string Country
);

public sealed record BasketTotalsResult(
    decimal SubtotalBeforeDiscounts,
    decimal ItemDiscounts,
    decimal DiscountCodeAmount,
    decimal SubtotalAfterDiscounts,
    decimal VatOnItems,
    decimal ShippingCost,
    decimal ShippingVat,
    decimal TotalWithoutVat,
    decimal TotalWithVat,
    decimal TotalSavings
);
