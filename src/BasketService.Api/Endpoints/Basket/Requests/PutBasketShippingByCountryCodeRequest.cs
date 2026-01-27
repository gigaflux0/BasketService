namespace BasketService.Api.Endpoints.Basket.Requests;

public sealed record PutBasketShippingByCountryCodeRequest(string BasketId, decimal Cost);