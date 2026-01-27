namespace BasketService.Api.Endpoints.Basket.Requests;

public sealed record PatchBasketItemsByIdRequest(string BasketId, int QuantityDelta);