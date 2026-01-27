namespace BasketService.Api.Endpoints.Basket.Requests;

public sealed record PostBasketItemsRequest(string BasketId, PostBasketItemsRequest.Item[] Items)
{
    public record Item(string ProductId, int Quantity);
};