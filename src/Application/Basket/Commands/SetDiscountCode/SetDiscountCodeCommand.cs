namespace Application.Basket.Commands.SetDiscountCode;

public sealed record SetDiscountCodeCommand(string BasketId, string Code);