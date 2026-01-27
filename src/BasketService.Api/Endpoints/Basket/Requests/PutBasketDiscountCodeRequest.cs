namespace BasketService.Api.Endpoints.Basket.Requests;

public sealed record PutBasketDiscountCodeRequest(string BasketId, string DiscountCode);