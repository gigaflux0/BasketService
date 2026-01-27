using Application.Basket.Commands.AddBasketItems;
using Application.Basket.Commands.AdjustBasketItemQuantity;
using Application.Basket.Commands.SetDiscountCode;
using Application.Basket.Commands.SetShippingCost;
using Application.Basket.Queries;
using BasketService.Api.Endpoints.Basket.Requests;
using BasketService.Api.Endpoints.Basket.Responses;

namespace BasketService.Api.Endpoints.Basket;

public static class BasketMappings
{
    public static AddBasketItemsCommand ToCommand(this PostBasketItemsRequest request) => new(
        request.BasketId,
        request.Items
            .Select(x => new AddBasketItemsCommand.Item(x.ProductId, x.Quantity))
            .ToList());

    public static AdjustBasketItemQuantityCommand ToCommand(this PatchBasketItemsByIdRequest request, string productId) =>
        new(
            request.BasketId,
            productId,
            request.QuantityDelta);

    public static SetDiscountCodeCommand ToCommand(this PutBasketDiscountCodeRequest request) => new(
        request.BasketId,
        request.DiscountCode);

    public static SetShippingCostCommand ToCommand(this PutBasketShippingByCountryCodeRequest request, string countryCode) => new(
        request.BasketId,
        countryCode,
        request.Cost);

    public static GetBasketTotalsByIdResponse ToResponse(this BasketTotalsResult queryResult) => new(
        queryResult.SubtotalBeforeDiscounts,
        queryResult.ItemDiscounts,
        queryResult.DiscountCodeAmount,
        queryResult.SubtotalAfterDiscounts,
        queryResult.VatOnItems,
        queryResult.ShippingCost,
        queryResult.ShippingVat,
        queryResult.TotalWithoutVat,
        queryResult.TotalWithVat,
        queryResult.TotalSavings);
}