namespace Application.Ports;

public sealed record BasketReadModel(
    string BasketId,
    string? DiscountCode,
    IReadOnlyList<BasketReadModelItem> Items,
    IReadOnlyDictionary<string, decimal> ShippingCosts
);

public sealed record BasketReadModelItem(
    string ProductId,
    int Quantity
);