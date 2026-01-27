namespace Application.Basket.Commands.SetShippingCost;

public sealed record SetShippingCostCommand(
    string BasketId,
    string CountryCode,
    decimal Cost
);