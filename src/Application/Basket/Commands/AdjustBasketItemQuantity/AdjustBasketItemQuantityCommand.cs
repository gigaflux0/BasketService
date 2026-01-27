namespace Application.Basket.Commands.AdjustBasketItemQuantity;

public sealed record AdjustBasketItemQuantityCommand(string BasketId, string ProductId, int QuantityDelta);