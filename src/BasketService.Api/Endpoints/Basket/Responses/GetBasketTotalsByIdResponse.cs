namespace BasketService.Api.Endpoints.Basket.Responses;

public sealed record GetBasketTotalsByIdResponse(
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